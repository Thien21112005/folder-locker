using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FolderLocker.Controllers;
using FolderLocker.Models;
using FolderLocker.Views.Theming;
using Guna.UI2.WinForms;

namespace FolderLocker.Views
{
    public partial class MainForm : Form
    {
        private const char MaskChar = '●';

        private readonly LockerController _controller = new LockerController();
        private string _selectedPath = "";
        private bool _isEncryptMode = true;
        private bool _isBusy;

        public MainForm()
        {
            InitializeComponent();
            AcceptButton = btnAction;
            ApplyFonts();

            Theme.Changed += OnThemeChanged;
            FormClosed += (_, _) => Theme.Changed -= OnThemeChanged;
            FormClosing += MainForm_FormClosing;
            Activated += (_, _) => UpdateCapsLock();

            toolTip.SetToolTip(btnSettings, "Cài đặt và trợ giúp");
            toolTip.SetToolTip(dropZone, "Nhấn Enter hoặc Ctrl+O để chọn");

            ResetForm();
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
            ThemeStyles.GradientButton(btnAction, p, _isEncryptMode);
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
            {
                if (files.Length > 1)
                    Dialogs.Info(this, "Đã chọn mục đầu tiên", $"Bạn thả {files.Length} mục; hiện chỉ xử lý: {Path.GetFileName(files[0])}");
                SetSelectedPath(files[0]);
            }
        }

        private void dropZone_Activated(object? sender, EventArgs e) => ChooseInput();

        private void ChooseInput()
        {
            if (_isBusy) return;

            var btnFile = new TaskDialogCommandLinkButton("Chọn file", "Mã hóa một file, hoặc giải mã một file .enc");
            var btnFolder = new TaskDialogCommandLinkButton("Chọn thư mục", "Mã hóa toàn bộ một thư mục");

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
                if (fileDialog.ShowDialog(this) == DialogResult.OK)
                    SetSelectedPath(fileDialog.FileName);
            }
            else if (result == btnFolder)
            {
                using var folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "Chọn thư mục cần mã hóa";
                folderDialog.UseDescriptionForTitle = true;
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                    SetSelectedPath(folderDialog.SelectedPath);
            }
        }

        private void SetSelectedPath(string path)
        {
            _selectedPath = path;
            bool isFolder = Directory.Exists(path);
            ApplyMode(encrypt: isFolder || !_controller.IsEncryptedFile(path));

            string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
            if (string.IsNullOrEmpty(name)) name = path;
            string kind = isFolder ? "Thư mục"
                : !_isEncryptMode ? "File đã mã hóa"
                : "File · " + FormatSize(new FileInfo(path).Length);

            lblSelectedPath.Text = name;
            lblSelectedPath.Visible = true;
            lblDragDrop.Text = kind;
            lblDragDrop.Font = Theme.Small;
            toolTip.SetToolTip(lblSelectedPath, path);
            dropZone.AccessibleDescription = $"Đã chọn: {name} ({kind}). Nhấn Enter để chọn lại.";

            lblIcon.Text = (_isEncryptMode ? (isFolder ? Theme.GlyphFolder : Theme.GlyphFile) : Theme.GlyphLock).ToString();
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

        /// <summary>Khóa/mở toàn bộ input trong lúc đang xử lý.</summary>
        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            dropZone.AllowDrop = !busy;
            dropZone.Enabled = !busy;
            txtPassword.Enabled = !busy;
            txtConfirmPassword.Enabled = !busy;
            chkDeleteOriginal.Enabled = !busy;
            btnAction.Enabled = !busy;
            btnSettings.Enabled = !busy;
            btnClose.Enabled = !busy;
            UseWaitCursor = busy;
            if (!busy) progressBar.Visible = false;
            UpdateFormHeight();
        }

        private void ResetForm()
        {
            _selectedPath = "";
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
                Dialogs.Info(this, "Đang xử lý dữ liệu", "Vui lòng đợi hoàn tất trước khi đóng.");
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
        // HÀNH ĐỘNG CHÍNH
        // ============================================

        private async void btnAction_Click(object? sender, EventArgs e)
        {
            if (_isBusy) return;

            if (string.IsNullOrEmpty(_selectedPath))
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

        private Progress<int> StartProgress()
        {
            SetBusy(true);
            progressBar.Value = 0;
            progressBar.Visible = true;
            UpdateFormHeight();
            return new Progress<int>(percent => progressBar.Value = Math.Clamp(percent, 0, 100));
        }

        private async Task EncryptData()
        {
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Lưu file mã hóa";
            saveDialog.Filter = "File đã mã hóa (*.enc)|*.enc";
            saveDialog.FileName = Path.GetFileName(Path.TrimEndingDirectorySeparator(_selectedPath)) + ".enc";

            if (saveDialog.ShowDialog(this) != DialogResult.OK) return;

            string dest = saveDialog.FileName;
            string source = _selectedPath;
            string pass = txtPassword.Text;
            bool deleteSrc = chkDeleteOriginal.Checked;

            var progress = StartProgress();
            OperationResult result;
            try
            {
                result = await Task.Run(() => _controller.Encrypt(source, dest, pass, progress));
            }
            finally
            {
                SetBusy(false);
            }

            if (!result.Success)
            {
                Dialogs.Error(this, "Mã hóa thất bại", result.Message);
                return;
            }

            RecoveryCodeDialog.ShowFor(this, [new RecoveryEntry(dest, result.RecoveryCode ?? "")]);

            // Chỉ xóa bản gốc sau khi người dùng đã thấy mã khôi phục
            if (deleteSrc)
            {
                var del = _controller.DeleteSource(source);
                if (!del.Success)
                    Dialogs.Warn(this, del.Message, "File mã hóa vẫn đã được tạo: " + dest);
            }

            ResetForm();
        }

        private async Task DecryptData()
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn nơi lưu dữ liệu sau khi giải mã";
            folderDialog.UseDescriptionForTitle = true;

            if (folderDialog.ShowDialog(this) != DialogResult.OK) return;

            string outFolder = folderDialog.SelectedPath;
            string source = _selectedPath;
            string pass = txtPassword.Text;

            var result = await RunDecrypt(source, outFolder, pass, checkOverwrite: true);

            if (result.NeedsOverwriteConfirm)
            {
                if (!Dialogs.Confirm(this, "Đích đã tồn tại", $"'{result.OutputPath}' đã tồn tại. Ghi đè sẽ thay thế hoàn toàn nội dung cũ.",
                        "Ghi đè", "Giữ nguyên", dangerous: true))
                    return;

                result = await RunDecrypt(source, outFolder, pass, checkOverwrite: false);
            }

            if (!result.Success)
            {
                Dialogs.Error(this, "Giải mã thất bại", result.Message);
                txtPassword.Focus();
                txtPassword.SelectAll();
                return;
            }

            Dialogs.SuccessWithOpen(this, "Giải mã thành công", result.OutputPath ?? outFolder);
            ResetForm();
        }

        private async Task<OperationResult> RunDecrypt(string source, string outFolder, string pass, bool checkOverwrite)
        {
            var progress = StartProgress();
            try
            {
                return await Task.Run(() => _controller.Decrypt(source, outFolder, pass, checkOverwrite, progress));
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}
