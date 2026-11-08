using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FolderLocker.Controllers;
using FolderLocker.Models;
using FolderLocker.Services;
using FolderLocker.Views.Theming;
using Guna.UI2.WinForms;

namespace FolderLocker.Views
{
    public partial class MainForm : Form
    {
        private const char MaskChar = '●';

        private readonly LockerController _controller = new LockerController();
        private readonly AppSettings _settings = AppSettings.Load();
        private List<string> _selectedPaths = new();
        private bool _isEncryptMode = true;
        private bool _isBusy;
        private CancellationTokenSource? _cts;
        private string _statusVerb = "";
        private bool _closeAfterCancel;
        private bool _holdClose;

        /// <summary>
        /// Cửa sổ không viền (FormBorderStyle.None) mặc định thiếu WS_MINIMIZEBOX nên Windows
        /// không chạy hiệu ứng thu nhỏ / hiện lại và bấm biểu tượng taskbar không thu nhỏ được.
        /// WS_EX_COMPOSITED vẽ toàn bộ cửa sổ một lần (double-buffer) để hết giật khi hiện lại.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x00020000;
                const int WS_SYSMENU = 0x00080000;
                const int WS_EX_COMPOSITED = 0x02000000;
                var cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_SYSMENU;
                cp.ExStyle |= WS_EX_COMPOSITED;
                return cp;
            }
        }

        public MainForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            InitializeComponent();
            AcceptButton = btnAction;
            ApplyFonts();

            Theme.Changed += OnThemeChanged;
            FormClosed += (_, _) => Theme.Changed -= OnThemeChanged;
            FormClosing += MainForm_FormClosing;
            Activated += (_, _) => UpdateCapsLock();

            toolTip.SetToolTip(btnSettings, "Cài đặt và trợ giúp");
            toolTip.SetToolTip(dropZone, "Nhấn Enter hoặc Ctrl+O để chọn");

            BuildSettingsMenu();
            ResetForm();
        }

        // ============================================
        // MENU CÀI ĐẶT (phần do code tạo)
        // ============================================

        private ToolStripMenuItem _mnuSecureDelete = null!;

        private void BuildSettingsMenu()
        {
            _mnuSecureDelete = new ToolStripMenuItem("Xóa vĩnh viễn bản gốc (ghi đè, không qua Thùng rác)")
            {
                Checked = _settings.SecureDelete,
            };
            _mnuSecureDelete.Click += (_, _) => ToggleSecureDelete();

            var mnuOpenLog = new ToolStripMenuItem("Mở thư mục nhật ký");
            mnuOpenLog.Click += (_, _) => OpenLogFolder();

            _mnuExplorer = new ToolStripMenuItem("Thêm vào menu chuột phải của Explorer");
            _mnuExplorer.Click += (_, _) => ToggleExplorerIntegration();
            try { _mnuExplorer.Checked = ShellIntegration.IsRegistered(); } catch { /* registry bị chặn */ }

            menuSettings.Items.Insert(0, _mnuSecureDelete);
            menuSettings.Items.Insert(1, _mnuExplorer);
            menuSettings.Items.Insert(2, new ToolStripSeparator());
            menuSettings.Items.Add(new ToolStripSeparator());
            menuSettings.Items.Add(mnuOpenLog);
            UpdateDeleteOptionText();
        }

        private ToolStripMenuItem _mnuExplorer = null!;

        private void ToggleExplorerIntegration()
        {
            try
            {
                if (_mnuExplorer.Checked)
                {
                    ShellIntegration.Unregister();
                    _mnuExplorer.Checked = false;
                    Dialogs.Info(this, "Đã gỡ khỏi Explorer", "Menu chuột phải và liên kết file .enc đã được gỡ.");
                }
                else
                {
                    string exe = Environment.ProcessPath ?? Application.ExecutablePath;
                    ShellIntegration.Register(exe);
                    _mnuExplorer.Checked = true;
                    Dialogs.Info(this, "Đã tích hợp với Explorer",
                        "• Chuột phải file/thư mục → \"Mã hóa bằng Folder Locker\" (Windows 11: trong \"Show more options\").\n" +
                        "• Double-click file .enc để mở bằng Folder Locker.\n\n" +
                        "Nếu bạn di chuyển thư mục ứng dụng, hãy tắt rồi bật lại tùy chọn này.");
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
            {
                Dialogs.Error(this, "Không thay đổi được tích hợp Explorer", ex.Message);
            }
        }

        // ============================================
        // NHẬN ĐƯỜNG DẪN TỪ DÒNG LỆNH / EXPLORER
        // ============================================

        private readonly IReadOnlyList<string> _initialPaths = Array.Empty<string>();
        private readonly List<string> _pendingExternal = new();
        private DateTime _lastExternalAt = DateTime.MinValue;

        /// <summary>Mở app với đường dẫn chọn sẵn (từ Explorer / dòng lệnh).</summary>
        public MainForm(IReadOnlyList<string> initialPaths) : this()
        {
            _initialPaths = initialPaths;
        }

        /// <summary>
        /// Nhận đường dẫn do tiến trình khác chuyển sang (an toàn khi gọi từ thread nền).
        /// Các lần nhận sát nhau (Explorer mở nhiều file) được gộp thành một lựa chọn.
        /// </summary>
        public void ReceiveExternalPaths(string[] paths)
        {
            lock (_pendingExternal) _pendingExternal.AddRange(paths);
            if (IsHandleCreated) BeginInvoke(FlushExternalPaths);
        }

        private void FlushExternalPaths()
        {
            string[] batch;
            lock (_pendingExternal)
            {
                batch = _pendingExternal.ToArray();
                _pendingExternal.Clear();
            }

            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            if (batch.Length == 0) return;

            if (_isBusy)
            {
                Dialogs.Info(this, "Đang xử lý", "Hãy đợi thao tác hiện tại xong rồi chọn lại.");
                return;
            }

            bool merge = DateTime.UtcNow - _lastExternalAt < TimeSpan.FromSeconds(2) && _selectedPaths.Count > 0;
            _lastExternalAt = DateTime.UtcNow;
            SetSelectedPaths(merge ? _selectedPaths.Concat(batch) : batch);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_initialPaths.Count > 0)
            {
                _lastExternalAt = DateTime.UtcNow;
                SetSelectedPaths(_initialPaths);
            }
            FlushExternalPaths();
        }

        private void OpenLogFolder()
        {
            try
            {
                Directory.CreateDirectory(AppLog.Folder);
                System.Diagnostics.Process.Start("explorer.exe", $"\"{AppLog.Folder}\"");
            }
            catch (Exception ex)
            {
                Dialogs.Error(this, "Không mở được thư mục nhật ký", ex.Message);
            }
        }

        private void ToggleSecureDelete()
        {
            bool enable = !_settings.SecureDelete;
            if (enable && !Dialogs.Confirm(this, "Bật xóa vĩnh viễn bản gốc?",
                    "Bản gốc sẽ được ghi đè bằng dữ liệu ngẫu nhiên rồi xóa, KHÔNG vào Thùng rác và không khôi phục được.\n\n" +
                    "Lưu ý: trên SSD, USB hoặc ổ có sao lưu/snapshot, dữ liệu cũ vẫn có thể còn sót ở nơi khác. " +
                    "Đây là biện pháp giảm rủi ro, không phải bảo đảm tuyệt đối.",
                    "Bật", "Không", dangerous: true))
                return;

            _settings.SecureDelete = enable;
            _settings.Save();
            _mnuSecureDelete.Checked = enable;
            UpdateDeleteOptionText();
        }

        private void UpdateDeleteOptionText()
        {
            chkDeleteOriginal.Text = _settings.SecureDelete
                ? "Xóa vĩnh viễn bản gốc sau khi mã hóa (không khôi phục được)"
                : "Xóa bản gốc sau khi mã hóa (chuyển vào Thùng rác)";
            chkDeleteOriginal.AccessibleName = chkDeleteOriginal.Text;
            toolTip.SetToolTip(chkDeleteOriginal, "Bản gốc chỉ bị xóa sau khi file .enc đã được đọc lại và kiểm tra thành công.");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateFormHeight();
            ActiveControl = dropZone;
        }

        // ============================================
        // THEME
        // ============================================

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            if (IsHandleCreated) BeginInvoke(ApplyTheme);
        }

        private void ApplyFonts()
        {
            lblTitle.Font = Theme.Heading;
            btnSettings.Font = Theme.IconFont(11F);
            btnSettings.Text = Theme.GlyphSettings.ToString();
            lblIcon.Font = Theme.IconFont(34F);
            lblDragDrop.Font = Theme.Body;
            lblSelectedPath.Font = Theme.BodyStrong;
            lblPassword.Font = Theme.Small;
            lblConfirm.Font = Theme.Small;
            txtPassword.Font = Theme.Body;
            txtConfirmPassword.Font = Theme.Body;
            lblCapsLock.Font = Theme.Small;
            lblStrength.Font = Theme.Small;
            lblMismatch.Font = Theme.Small;
            chkDeleteOriginal.Font = Theme.Small;
            btnAction.Font = Theme.BodyStrong;
            lblStatus.Font = Theme.Small;
            btnChangePassword.Font = Theme.Small;
        }

        private void ApplyTheme()
        {
            var p = Theme.Current;

            BackColor = p.Background;
            lblTitle.ForeColor = p.TextPrimary;
            ThemeStyles.LinkButton(btnSettings, p);
            btnSettings.ForeColor = p.TextSecondary;
            ThemeStyles.ControlBox(btnMinimize, p);
            ThemeStyles.ControlBox(btnClose, p);
            ThemeStyles.Menu(menuSettings, p);

            dropZone.SetColors(p.Border, p.Focus, p.Surface, p.DropTint);
            lblDragDrop.ForeColor = p.TextSecondary;
            lblSelectedPath.ForeColor = p.TextPrimary;
            lblIcon.ForeColor = _isEncryptMode ? p.Link : p.Success;

            lblPassword.ForeColor = p.TextPrimary;
            lblConfirm.ForeColor = p.TextPrimary;
            ThemeStyles.TextBox(txtPassword, p);
            ThemeStyles.TextBox(txtConfirmPassword, p);
            lblCapsLock.ForeColor = p.Warning;
            ThemeStyles.CheckBox(chkDeleteOriginal, p);
            if (_isBusy) ThemeStyles.NeutralGradientButton(btnAction, p);
            else ThemeStyles.GradientButton(btnAction, p, _isEncryptMode);
            lblStatus.ForeColor = p.TextSecondary;
            ThemeStyles.LinkButton(btnChangePassword, p);
            ThemeStyles.ProgressBar(progressBar, p, _isEncryptMode ? p.EncryptFrom : p.Success);

            // Icon con mắt vẽ lại theo màu theme
            SetMasked(txtPassword, IsMasked(txtPassword));
            SetMasked(txtConfirmPassword, IsMasked(txtConfirmPassword));

            UpdateValidation();
            Invalidate(true);
        }

        // ============================================
        // CHỌN FILE / FOLDER
        // ============================================

        private void dropZone_DragEnter(object? sender, DragEventArgs e)
        {
            if (!_isBusy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                dropZone.IsDragTarget = true;
            }
        }

        private void dropZone_DragLeave(object? sender, EventArgs e) => dropZone.IsDragTarget = false;

        private void dropZone_DragDrop(object? sender, DragEventArgs e)
        {
            dropZone.IsDragTarget = false;
            if (_isBusy) return;
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                SetSelectedPaths(files);
        }

        private void dropZone_Activated(object? sender, EventArgs e) => ChooseInput();

        private void ChooseInput()
        {
            if (_isBusy) return;

            var btnFile = new TaskDialogCommandLinkButton("Chọn file", "Mã hóa file, hoặc giải mã file .enc. Giữ Ctrl để chọn nhiều file.");
            var btnFolder = new TaskDialogCommandLinkButton("Chọn thư mục", "Mã hóa toàn bộ thư mục. Giữ Ctrl để chọn nhiều thư mục.");

            var page = new TaskDialogPage()
            {
                Caption = "Folder Locker",
                Heading = "Bạn muốn chọn gì?",
                Buttons = new TaskDialogButtonCollection() { btnFile, btnFolder, TaskDialogButton.Cancel },
                AllowCancel = true,
            };

            var result = TaskDialog.ShowDialog(this, page);

            if (result == btnFile)
            {
                using var fileDialog = new OpenFileDialog();
                fileDialog.Title = "Chọn file";
                fileDialog.Filter = "Tất cả file (*.*)|*.*|File đã mã hóa (*.enc)|*.enc";
                fileDialog.Multiselect = true;
                if (fileDialog.ShowDialog(this) == DialogResult.OK)
                    SetSelectedPaths(fileDialog.FileNames);
            }
            else if (result == btnFolder)
            {
                using var folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "Chọn thư mục cần mã hóa";
                folderDialog.UseDescriptionForTitle = true;
                folderDialog.Multiselect = true;
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                    SetSelectedPaths(folderDialog.SelectedPaths);
            }
        }

        private static string DisplayName(string path)
        {
            string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
            return string.IsNullOrEmpty(name) ? path : name;
        }

        /// <summary>
        /// Nhận một hoặc nhiều mục. Toàn file .enc → giải mã; toàn file/thư mục thường → mã hóa;
        /// trộn lẫn hai loại thì từ chối để tránh nhầm thao tác.
        /// </summary>
        internal void SetSelectedPaths(IEnumerable<string> paths)
        {
            var items = paths.Where(p => File.Exists(p) || Directory.Exists(p))
                             .Select(Path.GetFullPath)
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToList();
            if (items.Count == 0) return;

            var encrypted = items.Where(p => File.Exists(p) && _controller.IsEncryptedFile(p)).ToList();
            if (encrypted.Count > 0 && encrypted.Count < items.Count)
            {
                Dialogs.Warn(this, "Không thể vừa mã hóa vừa giải mã",
                    $"Bạn đã chọn {encrypted.Count} file đã mã hóa và {items.Count - encrypted.Count} mục chưa mã hóa. " +
                    "Hãy chọn toàn file .enc để giải mã, hoặc toàn file/thư mục thường để mã hóa.");
                return;
            }

            _selectedPaths = items;
            ApplyMode(encrypt: encrypted.Count == 0);

            string title, kind;
            char glyph;
            if (items.Count == 1)
            {
                string path = items[0];
                bool isFolder = Directory.Exists(path);
                title = DisplayName(path);
                kind = isFolder ? "Thư mục"
                    : !_isEncryptMode ? "File đã mã hóa"
                    : "File · " + FormatSize(new FileInfo(path).Length);
                glyph = _isEncryptMode ? (isFolder ? Theme.GlyphFolder : Theme.GlyphFile) : Theme.GlyphLock;
                toolTip.SetToolTip(lblSelectedPath, path);
            }
            else
            {
                int folders = items.Count(Directory.Exists);
                int files = items.Count - folders;
                title = $"{items.Count} mục đã chọn";
                kind = !_isEncryptMode ? $"{items.Count} file đã mã hóa"
                    : string.Join(" · ", new[] { files > 0 ? $"{files} file" : null, folders > 0 ? $"{folders} thư mục" : null }.Where(s => s != null));
                glyph = _isEncryptMode ? Theme.GlyphMultiple : Theme.GlyphLock;
                var preview = items.Take(15).Select(DisplayName).ToList();
                if (items.Count > 15) preview.Add($"… và {items.Count - 15} mục khác");
                toolTip.SetToolTip(lblSelectedPath, string.Join(Environment.NewLine, preview));
            }

            lblSelectedPath.Text = title;
            lblSelectedPath.Visible = true;
            lblDragDrop.Text = kind;
            lblDragDrop.Font = Theme.Small;
            dropZone.AccessibleDescription = $"Đã chọn: {title} ({kind}). Nhấn Enter để chọn lại.";
            lblIcon.Text = glyph.ToString();
            lblIcon.ForeColor = _isEncryptMode ? Theme.Current.Link : Theme.Current.Success;

            txtPassword.Focus();
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
            return unit == 0 ? $"{bytes} B" : $"{size:0.#} {units[unit]}";
        }

        // ============================================
        // TRẠNG THÁI FORM (một nơi duy nhất quản lý)
        // ============================================

        /// <summary>Chuyển giao diện giữa chế độ mã hóa và giải mã.</summary>
        private void ApplyMode(bool encrypt)
        {
            _isEncryptMode = encrypt;

            btnAction.Text = encrypt ? "Mã hóa dữ liệu" : "Giải mã dữ liệu";
            btnAction.AccessibleName = btnAction.Text;

            // Giải mã: một ô duy nhất nhận cả mật khẩu lẫn mã khôi phục
            lblPassword.Text = encrypt ? "Mật khẩu" : "Mật khẩu hoặc mã khôi phục";
            txtPassword.AccessibleName = lblPassword.Text;
            txtPassword.PlaceholderText = encrypt
                ? $"Tối thiểu {PasswordStrength.MinLength} ký tự"
                : "Nhập mật khẩu hoặc mã FL-XXXX-XXXX-XXXX";

            strengthBar.Visible = encrypt;
            lblStrength.Visible = encrypt;
            lblConfirm.Visible = encrypt;
            txtConfirmPassword.Visible = encrypt;
            lblMismatch.Visible = encrypt;
            chkDeleteOriginal.Visible = encrypt;
            btnChangePassword.Visible = !encrypt && _selectedPaths.Count == 1;

            SetMasked(txtPassword, true);
            SetMasked(txtConfirmPassword, true);

            var p = Theme.Current;
            ThemeStyles.GradientButton(btnAction, p, encrypt);
            ThemeStyles.ProgressBar(progressBar, p, encrypt ? p.EncryptFrom : p.Success);

            UpdateValidation();
            UpdateFormHeight();
        }

        private void UpdateFormHeight()
        {
            flowMain.PerformLayout();
            ClientSize = new Size(ClientSize.Width, flowMain.Bottom + LogicalToDeviceUnits(22));
        }

        private void SetMasked(Guna2TextBox box, bool masked)
        {
            box.UseSystemPasswordChar = masked;
            box.PasswordChar = masked ? MaskChar : '\0';
            int px = LogicalToDeviceUnits(20);
            box.IconRight = Theme.Glyph(masked ? Theme.GlyphView : Theme.GlyphHide, Theme.Current.TextSecondary, px);
            box.IconRightSize = new Size(px, px);
            toolTip.SetToolTip(box, masked ? "Hiện mật khẩu (Alt+H)" : "Ẩn mật khẩu (Alt+H)");
        }

        private static bool IsMasked(Guna2TextBox box) =>
            box.PasswordChar == MaskChar || box.UseSystemPasswordChar;

        /// <summary>Độ mạnh mật khẩu + kiểm tra khớp ngay khi gõ.</summary>
        private void UpdateValidation()
        {
            var p = Theme.Current;

            if (_isEncryptMode)
            {
                var level = PasswordStrength.Evaluate(txtPassword.Text);
                Color color = level switch
                {
                    StrengthLevel.TooShort or StrengthLevel.Weak => p.Danger,
                    StrengthLevel.Fair => p.Warning,
                    _ => p.Success,
                };
                strengthBar.Value = PasswordStrength.Percent(level);
                ThemeStyles.ProgressBar(strengthBar, p, color);
                lblStrength.Text = level == StrengthLevel.Empty ? "" : "Độ mạnh: " + PasswordStrength.Describe(level);
                lblStrength.ForeColor = color;
                strengthBar.AccessibleDescription = lblStrength.Text;

                if (txtConfirmPassword.Text.Length == 0)
                {
                    lblMismatch.Text = "";
                }
                else if (txtConfirmPassword.Text == txtPassword.Text)
                {
                    lblMismatch.Text = "✓ Mật khẩu đã khớp";
                    lblMismatch.ForeColor = p.Success;
                }
                else
                {
                    lblMismatch.Text = "Mật khẩu xác nhận chưa khớp";
                    lblMismatch.ForeColor = p.Danger;
                }
            }

            UpdateCapsLock();
        }

        private void UpdateCapsLock()
        {
            bool typing = txtPassword.ContainsFocus || txtConfirmPassword.ContainsFocus;
            lblCapsLock.Text = typing && IsKeyLocked(Keys.CapsLock) ? "⚠ Caps Lock đang bật" : "";
        }

        private void PasswordFields_TextChanged(object? sender, EventArgs e) => UpdateValidation();
        private void PasswordFields_KeyUp(object? sender, KeyEventArgs e) => UpdateCapsLock();
        private void PasswordFields_Enter(object? sender, EventArgs e) => UpdateCapsLock();

        /// <summary>
        /// Khóa/mở input trong lúc đang xử lý. Khi bận, nút chính trở thành nút "Hủy".
        /// </summary>
        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            dropZone.AllowDrop = !busy;
            dropZone.Enabled = !busy;
            txtPassword.Enabled = !busy;
            txtConfirmPassword.Enabled = !busy;
            chkDeleteOriginal.Enabled = !busy;
            btnSettings.Enabled = !busy;
            btnChangePassword.Enabled = !busy;
            btnAction.Enabled = true;

            var p = Theme.Current;
            if (busy)
            {
                btnAction.Text = "Hủy";
                btnAction.AccessibleName = "Hủy thao tác đang chạy";
                ThemeStyles.NeutralGradientButton(btnAction, p);
                AcceptButton = null;
            }
            else
            {
                btnAction.Text = _isEncryptMode ? "Mã hóa dữ liệu" : "Giải mã dữ liệu";
                btnAction.AccessibleName = btnAction.Text;
                ThemeStyles.GradientButton(btnAction, p, _isEncryptMode);
                AcceptButton = btnAction;
                lblStatus.Visible = false;
                progressBar.Visible = false;
            }
            UpdateFormHeight();

            if (!busy && _closeAfterCancel && !_holdClose) BeginInvoke(Close);
        }

        /// <summary>Vào trạng thái bận (một lần cho cả lô); trả về token để hủy.</summary>
        private CancellationToken BeginBusy(string verb, bool showProgress = true)
        {
            _cts = new CancellationTokenSource();
            SetBusy(true);
            progressBar.Value = 0;
            progressBar.Visible = showProgress;
            lblStatus.Visible = true;
            SetStatus(verb);
            UpdateFormHeight();
            return _cts.Token;
        }

        private void EndBusy()
        {
            _cts?.Dispose();
            _cts = null;
            SetBusy(false);
        }

        private void SetStatus(string verb)
        {
            _statusVerb = verb;
            lblStatus.Text = verb + "…";
        }

        /// <summary>Tiến độ của mục thứ <paramref name="index"/> trong lô <paramref name="count"/> mục, quy ra tổng %.</summary>
        private IProgress<int> MakeProgress(int index = 0, int count = 1) => new Progress<int>(percent =>
        {
            int overall = (index * 100 + Math.Clamp(percent, 0, 100)) / Math.Max(1, count);
            progressBar.Value = Math.Clamp(overall, 0, 100);
            lblStatus.Text = $"{_statusVerb}… {overall}%";
        });

        /// <summary>
        /// Chạy một thao tác nền: hiện trạng thái + tiến độ, hỗ trợ hủy, luôn mở khóa form khi xong.
        /// </summary>
        private async Task<OperationResult> RunOperation(string verb,
            Func<IProgress<int>, CancellationToken, OperationResult> work, bool showProgress = true)
        {
            var token = BeginBusy(verb, showProgress);
            var progress = MakeProgress();
            try
            {
                return await Task.Run(() => work(progress, token));
            }
            finally
            {
                EndBusy();
            }
        }

        private void ResetForm()
        {
            _selectedPaths = new();
            lblSelectedPath.Text = "";
            lblSelectedPath.Visible = false;
            lblDragDrop.Text = "Kéo thả file hoặc thư mục vào đây\nhoặc nhấn để chọn";
            lblDragDrop.Font = Theme.Body;
            lblIcon.Text = Theme.GlyphUpload.ToString();
            dropZone.AccessibleDescription = "Nhấn Enter để chọn, hoặc kéo thả file hay thư mục vào đây.";
            txtPassword.Clear();
            txtConfirmPassword.Clear();
            ApplyMode(encrypt: true);
            ApplyTheme();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isBusy && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                if (_closeAfterCancel) return;
                if (Dialogs.Confirm(this, "Đang xử lý dữ liệu",
                        "Hủy thao tác đang chạy và đóng ứng dụng? File đang tạo dở sẽ được dọn sạch.",
                        "Hủy và đóng", "Tiếp tục chạy", dangerous: true))
                {
                    _closeAfterCancel = true;
                    _cts?.Cancel();
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!_isBusy)
            {
                switch (keyData)
                {
                    case Keys.Control | Keys.O:
                        ChooseInput();
                        return true;
                    case Keys.Alt | Keys.H:
                        var box = txtConfirmPassword.ContainsFocus ? txtConfirmPassword : txtPassword;
                        SetMasked(box, !IsMasked(box));
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void txtPassword_IconRightClick(object? sender, EventArgs e) =>
            SetMasked(txtPassword, !IsMasked(txtPassword));

        private void txtConfirmPassword_IconRightClick(object? sender, EventArgs e) =>
            SetMasked(txtConfirmPassword, !IsMasked(txtConfirmPassword));

        // ============================================
        // MENU CÀI ĐẶT / TRỢ GIÚP
        // ============================================

        private void btnSettings_Click(object? sender, EventArgs e) =>
            menuSettings.Show(btnSettings, new Point(btnSettings.Width - menuSettings.Width, btnSettings.Height));

        private void mnuHelpRecovery_Click(object? sender, EventArgs e) =>
            Dialogs.Info(this, "Mã khôi phục là gì?",
                "Mỗi lần mã hóa, Folder Locker tạo một mã khôi phục dạng FL-XXXX-XXXX-XXXX.\n\n" +
                "• Mã này mở được file .enc khi bạn quên mật khẩu.\n" +
                "• Mã chỉ hiển thị một lần, ngay sau khi mã hóa xong.\n" +
                "• Ai có mã đều mở được file, nên hãy cất ở nơi an toàn, tách biệt với file .enc.\n\n" +
                "Khi giải mã, nhập mật khẩu hoặc mã khôi phục vào cùng một ô.");

        private void mnuHelpShortcuts_Click(object? sender, EventArgs e) =>
            Dialogs.Info(this, "Phím tắt",
                "Ctrl+O\tChọn file hoặc thư mục\n" +
                "Enter\tMã hóa / giải mã\n" +
                "Alt+H\tHiện / ẩn mật khẩu đang nhập\n" +
                "Tab\tChuyển giữa các ô");

        // ============================================
        // ĐỔI MẬT KHẨU (F1)
        // ============================================

        private async void btnChangePassword_Click(object? sender, EventArgs e)
        {
            if (_isBusy || _selectedPaths.Count != 1) return;
            string enc = _selectedPaths[0];

            try
            {
                using var dlg = new ChangePasswordDialog(enc, txtPassword.Text);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string current = dlg.CurrentSecret, next = dlg.NewPassword;
                bool rotate = dlg.NewRecoveryCode;

                var result = await RunOperation("Đang đổi mật khẩu",
                    (_, ct) => _controller.ChangePassword(enc, current, next, rotate, ct), showProgress: false);
                if (_closeAfterCancel) return;

                if (!result.Success)
                {
                    Dialogs.Error(this, "Không đổi được mật khẩu", result.Message);
                    return;
                }

                if (result.RecoveryCode != null)
                    RecoveryCodeDialog.ShowFor(this, [new RecoveryEntry(enc, result.RecoveryCode)]);

                Dialogs.Info(this, "Đã đổi mật khẩu",
                    rotate ? "Từ giờ hãy dùng mật khẩu mới hoặc mã khôi phục mới." : "Từ giờ hãy dùng mật khẩu mới. Mã khôi phục cũ vẫn dùng được.");
                txtPassword.Clear();
                txtPassword.Focus();
            }
            catch (Exception ex)
            {
                SetBusy(false);
                Dialogs.Error(this, "Đã xảy ra lỗi", ex.Message);
            }
        }

        // ============================================
        // HÀNH ĐỘNG CHÍNH
        // ============================================

        private async void btnAction_Click(object? sender, EventArgs e)
        {
            if (_isBusy)
            {
                // Đang chạy → nút chính là "Hủy"
                if (_cts is { IsCancellationRequested: false })
                {
                    _cts.Cancel();
                    btnAction.Text = "Đang hủy…";
                    btnAction.Enabled = false;
                }
                return;
            }

            if (_selectedPaths.Count == 0)
            {
                Dialogs.Warn(this, "Chưa chọn dữ liệu", "Hãy kéo thả hoặc chọn một file / thư mục trước.");
                dropZone.Focus();
                return;
            }

            if (!ValidateInputs()) return;

            try
            {
                if (_isEncryptMode)
                    await EncryptData();
                else
                    await DecryptData();
            }
            catch (Exception ex)
            {
                // async void: exception không được bắt sẽ làm sập app
                SetBusy(false);
                Dialogs.Error(this, "Đã xảy ra lỗi", ex.Message);
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                Dialogs.Warn(this, _isEncryptMode ? "Chưa nhập mật khẩu" : "Chưa nhập mật khẩu hoặc mã khôi phục");
                txtPassword.Focus();
                return false;
            }

            if (!_isEncryptMode) return true;

            var level = PasswordStrength.Evaluate(txtPassword.Text);
            if (level == StrengthLevel.TooShort)
            {
                Dialogs.Warn(this, "Mật khẩu quá ngắn", $"Mật khẩu cần tối thiểu {PasswordStrength.MinLength} ký tự.");
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                Dialogs.Warn(this, "Mật khẩu xác nhận chưa khớp", "Hãy nhập lại mật khẩu ở ô xác nhận.");
                txtConfirmPassword.Focus();
                return false;
            }

            if (level == StrengthLevel.Weak &&
                !Dialogs.Confirm(this, "Mật khẩu yếu",
                    "Mật khẩu này dễ bị đoán (quá phổ biến hoặc lặp lại). Nên dùng cụm từ dài, có chữ hoa, số và ký tự đặc biệt.",
                    "Vẫn dùng mật khẩu này", "Đổi mật khẩu", dangerous: true))
            {
                txtPassword.Focus();
                return false;
            }

            return true;
        }

        private Task EncryptData() =>
            _selectedPaths.Count == 1 ? EncryptSingle(_selectedPaths[0]) : EncryptBatch(_selectedPaths.ToList());

        private Task DecryptData() =>
            _selectedPaths.Count == 1 ? DecryptSingle(_selectedPaths[0]) : DecryptBatch(_selectedPaths.ToList());

        // ============================================
        // MỘT MỤC
        // ============================================

        private async Task EncryptSingle(string source)
        {
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Lưu file mã hóa";
            saveDialog.Filter = "File đã mã hóa (*.enc)|*.enc";
            saveDialog.FileName = DisplayName(source) + ".enc";

            if (saveDialog.ShowDialog(this) != DialogResult.OK) return;

            string dest = saveDialog.FileName;
            string pass = txtPassword.Text;
            bool deleteSrc = chkDeleteOriginal.Checked;

            var result = await RunOperation("Đang mã hóa", (pr, ct) => _controller.Encrypt(source, dest, pass, pr, ct));
            if (_closeAfterCancel) return;

            if (result.Cancelled)
            {
                Dialogs.Info(this, "Đã hủy mã hóa", result.Message);
                return;
            }

            if (!result.Success)
            {
                Dialogs.Error(this, "Mã hóa thất bại", result.Message);
                return;
            }

            RecoveryCodeDialog.ShowFor(this, [new RecoveryEntry(dest, result.RecoveryCode ?? "")]);

            // Chỉ xóa bản gốc sau khi người dùng đã thấy mã khôi phục và file .enc đã được kiểm tra lại
            if (deleteSrc)
            {
                string? problem = await DeleteOriginalAfterVerify(source, dest, pass);
                if (_closeAfterCancel) return;
                if (problem != null)
                    Dialogs.Warn(this, "Bản gốc chưa bị xóa", problem + "\n\nFile mã hóa: " + dest);
            }

            ResetForm();
        }

        /// <summary>
        /// Đọc lại toàn bộ file .enc (xác thực mọi chunk) rồi mới xóa bản gốc.
        /// Trả về null nếu đã xóa; ngược lại là lý do giữ nguyên bản gốc.
        /// </summary>
        private async Task<string?> DeleteOriginalAfterVerify(string source, string dest, string pass)
        {
            var verify = await RunOperation($"Đang kiểm tra lại {Path.GetFileName(dest)}", (pr, ct) => _controller.Verify(dest, pass, pr, ct));
            if (_closeAfterCancel) return null;
            if (!verify.Success)
                return verify.Cancelled
                    ? "Bạn đã dừng bước kiểm tra nên bản gốc được giữ nguyên."
                    : "File mã hóa không vượt qua bước kiểm tra nên bản gốc được giữ nguyên. " + verify.Message;

            var del = _settings.SecureDelete
                ? await RunOperation($"Đang xóa an toàn {DisplayName(source)}", (pr, ct) => _controller.DeleteSource(source, secure: true, pr, ct))
                : _controller.DeleteSource(source);
            return del.Success ? null : del.Message;
        }

        /// <summary>Hỏi thư mục xuất (mở sẵn thư mục lần trước); trả về "" nếu người dùng hủy.</summary>
        private string ChooseOutputFolder(string description, string fallbackNear)
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = description;
            folderDialog.UseDescriptionForTitle = true;
            folderDialog.InitialDirectory = Directory.Exists(_settings.LastOutputFolder)
                ? _settings.LastOutputFolder
                : Path.GetDirectoryName(Path.GetFullPath(fallbackNear)) ?? "";

            if (folderDialog.ShowDialog(this) != DialogResult.OK) return "";

            _settings.LastOutputFolder = folderDialog.SelectedPath;
            _settings.Save();
            return folderDialog.SelectedPath;
        }

        private async Task DecryptSingle(string source)
        {
            string pass = txtPassword.Text;

            // Kiểm tra mật khẩu trước, để nhập sai không phải chọn lại thư mục mỗi lần
            var check = await RunOperation("Đang kiểm tra mật khẩu", (_, _) => _controller.CheckSecret(source, pass), showProgress: false);
            if (_closeAfterCancel) return;
            if (!check.Success)
            {
                Dialogs.Error(this, "Không mở được file", check.Message);
                txtPassword.Focus();
                txtPassword.SelectAll();
                return;
            }

            string outFolder = ChooseOutputFolder("Chọn nơi lưu dữ liệu sau khi giải mã", source);
            if (outFolder.Length == 0) return;

            var result = await RunOperation("Đang giải mã", (pr, ct) => _controller.Decrypt(source, outFolder, pass, true, pr, ct));
            if (_closeAfterCancel) return;

            if (result.NeedsOverwriteConfirm)
            {
                if (!Dialogs.AskOverwrite(this, result.OutputPath!, remaining: 0).overwrite)
                    return;

                result = await RunOperation("Đang giải mã", (pr, ct) => _controller.Decrypt(source, outFolder, pass, false, pr, ct));
                if (_closeAfterCancel) return;
            }

            if (result.Cancelled)
            {
                Dialogs.Info(this, "Đã hủy giải mã", result.Message);
                return;
            }

            if (!result.Success)
            {
                Dialogs.Error(this, "Giải mã thất bại", result.Message);
                return;
            }

            Dialogs.SuccessWithOpen(this, "Giải mã thành công", result.OutputPath ?? outFolder);
            ResetForm();
        }

        // ============================================
        // HÀNG LOẠT (F5)
        // ============================================

        private async Task EncryptBatch(List<string> sources)
        {
            string outDir = ChooseOutputFolder($"Chọn nơi lưu {sources.Count} file mã hóa", sources[0]);
            if (outDir.Length == 0) return;

            // Đích <tên>.enc; các mục trùng tên được đánh số để không ghi đè lẫn nhau
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var jobs = new List<(string Source, string Dest)>();
            foreach (var src in sources)
            {
                string baseName = DisplayName(src);
                string dest = Path.Combine(outDir, baseName + ".enc");
                for (int n = 2; !used.Add(dest); n++) dest = Path.Combine(outDir, $"{baseName} ({n}).enc");
                jobs.Add((src, dest));
            }

            var existing = jobs.Where(j => File.Exists(j.Dest)).ToList();
            if (existing.Count > 0)
            {
                string list = string.Join("\n", existing.Take(8).Select(j => "• " + Path.GetFileName(j.Dest))) + (existing.Count > 8 ? "\n…" : "");
                if (!Dialogs.Confirm(this, $"{existing.Count} file .enc đã tồn tại", list + "\n\nGhi đè các file này?",
                        "Ghi đè", "Bỏ qua các file trùng", dangerous: true))
                    jobs = jobs.Except(existing).ToList();
            }
            if (jobs.Count == 0) return;

            string pass = txtPassword.Text;
            bool deleteSrc = chkDeleteOriginal.Checked;
            var done = new List<(string Source, string Dest, string Code)>();
            var failures = new List<(string Name, string Reason)>();
            bool cancelled = false;

            // Giữ cửa sổ mở tới khi người dùng đã thấy các mã khôi phục, kể cả khi họ bấm đóng giữa chừng
            _holdClose = true;
            try
            {
                var ct = BeginBusy("Đang mã hóa");
                try
                {
                    for (int i = 0; i < jobs.Count; i++)
                    {
                        var (src, dest) = jobs[i];
                        SetStatus($"Đang mã hóa {i + 1}/{jobs.Count}: {DisplayName(src)}");
                        var progress = MakeProgress(i, jobs.Count);
                        var r = await Task.Run(() => _controller.Encrypt(src, dest, pass, progress, ct));
                        if (r.Cancelled) { cancelled = true; break; }
                        if (r.Success) done.Add((src, dest, r.RecoveryCode!));
                        else failures.Add((DisplayName(src), r.Message));
                    }
                }
                finally
                {
                    EndBusy();
                }

                if (done.Count > 0)
                    RecoveryCodeDialog.ShowFor(this, done.Select(d => new RecoveryEntry(d.Dest, d.Code)).ToList());

                if (deleteSrc && !_closeAfterCancel)
                {
                    foreach (var d in done)
                    {
                        string? problem = await DeleteOriginalAfterVerify(d.Source, d.Dest, pass);
                        if (_closeAfterCancel) break;
                        if (problem != null) failures.Add((DisplayName(d.Source), "Bản gốc chưa bị xóa. " + problem));
                    }
                }
            }
            finally
            {
                _holdClose = false;
            }

            if (_closeAfterCancel)
            {
                Close();
                return;
            }

            // Thành công trọn vẹn thì dialog mã khôi phục đã là màn hình kết thúc
            if (failures.Count > 0 || cancelled)
                Dialogs.BatchSummary(this, "mã hóa", done.Count, jobs.Count, failures, cancelled, outDir);
            ResetForm();
        }

        private async Task DecryptBatch(List<string> sources)
        {
            string pass = txtPassword.Text;
            var failures = new List<(string Name, string Reason)>();
            var unlocked = new List<string>();

            var ct = BeginBusy("Đang kiểm tra mật khẩu");
            try
            {
                for (int i = 0; i < sources.Count && !ct.IsCancellationRequested; i++)
                {
                    string p = sources[i];
                    SetStatus($"Đang kiểm tra mật khẩu {i + 1}/{sources.Count}");
                    var r = await Task.Run(() => _controller.CheckSecret(p, pass));
                    MakeProgress(i, sources.Count).Report(100);
                    if (r.Success) unlocked.Add(p);
                    else failures.Add((DisplayName(p), r.Message));
                }
            }
            finally
            {
                EndBusy();
            }
            if (_closeAfterCancel || ct.IsCancellationRequested) return;

            if (unlocked.Count == 0)
            {
                Dialogs.Error(this, "Không mở được file nào", failures[0].Reason);
                txtPassword.Focus();
                txtPassword.SelectAll();
                return;
            }

            if (failures.Count > 0)
            {
                string list = string.Join("\n", failures.Take(8).Select(f => "• " + f.Name)) + (failures.Count > 8 ? "\n…" : "");
                if (!Dialogs.Confirm(this, $"Mật khẩu không đúng với {failures.Count} file", list,
                        $"Giải mã {unlocked.Count} file còn lại", "Dừng"))
                    return;
            }

            string outFolder = ChooseOutputFolder($"Chọn nơi lưu dữ liệu của {unlocked.Count} file", unlocked[0]);
            if (outFolder.Length == 0) return;

            int ok = 0;
            bool cancelled = false;
            bool? overwriteAll = null;

            ct = BeginBusy("Đang giải mã");
            try
            {
                for (int i = 0; i < unlocked.Count; i++)
                {
                    string p = unlocked[i];
                    string name = DisplayName(p);
                    SetStatus($"Đang giải mã {i + 1}/{unlocked.Count}: {name}");
                    var progress = MakeProgress(i, unlocked.Count);
                    bool check = overwriteAll != true;
                    var r = await Task.Run(() => _controller.Decrypt(p, outFolder, pass, check, progress, ct));

                    if (r.NeedsOverwriteConfirm)
                    {
                        bool overwrite;
                        if (overwriteAll.HasValue)
                        {
                            overwrite = overwriteAll.Value;
                        }
                        else
                        {
                            var answer = Dialogs.AskOverwrite(this, r.OutputPath!, remaining: unlocked.Count - i - 1);
                            overwrite = answer.overwrite;
                            if (answer.applyToAll) overwriteAll = overwrite;
                        }

                        if (!overwrite)
                        {
                            failures.Add((name, "Bỏ qua vì đích đã tồn tại."));
                            continue;
                        }
                        r = await Task.Run(() => _controller.Decrypt(p, outFolder, pass, false, progress, ct));
                    }

                    if (r.Cancelled) { cancelled = true; break; }
                    if (r.Success) ok++;
                    else failures.Add((name, r.Message));
                }
            }
            finally
            {
                EndBusy();
            }
            if (_closeAfterCancel) return;

            Dialogs.BatchSummary(this, "giải mã", ok, sources.Count, failures, cancelled, outFolder);
            if (ok > 0) ResetForm();
        }
    }
}
