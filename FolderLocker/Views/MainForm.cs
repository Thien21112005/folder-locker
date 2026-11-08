using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FolderLocker.Controllers;
using FolderLocker.Models;

namespace FolderLocker.Views
{
    public partial class MainForm : Form
    {
        private const char MaskChar = '●';

        private static readonly Color EncryptIconColor = Color.FromArgb(94, 148, 255);
        private static readonly Color DecryptIconColor = Color.FromArgb(46, 204, 113);

        // Cache 2 bitmap icon con mắt (trước đây tạo Bitmap mới mỗi lần bấm → rò rỉ GDI)
        private static readonly Bitmap EyeOpenIcon = DrawEyeIcon(true);
        private static readonly Bitmap EyeClosedIcon = DrawEyeIcon(false);

        private readonly LockerController _controller = new LockerController();
        private string _selectedPath = "";
        private bool _isEncryptMode = true;
        private bool _isRecoveryMode = false;
        private bool _isBusy = false;

        public MainForm()
        {
            InitializeComponent();
            AcceptButton = btnAction;
            FormClosing += MainForm_FormClosing;
            ResetForm();
        }

        // ============================================
        // CHỌN FILE / FOLDER
        // ============================================

        private void pnlDragDrop_DragEnter(object? sender, DragEventArgs e)
        {
            if (!_isBusy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void pnlDragDrop_DragDrop(object? sender, DragEventArgs e)
        {
            if (_isBusy) return;
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                SetSelectedPath(files[0]);
        }

        private void pnlDragDrop_Click(object? sender, EventArgs e)
        {
            if (_isBusy) return;

            var btnFile = new TaskDialogCommandLinkButton("Chọn File", "Mã hóa hoặc giải mã một tập tin đơn lẻ");
            var btnFolder = new TaskDialogCommandLinkButton("Chọn Thư mục (Folder)", "Mã hóa hoặc giải mã toàn bộ một thư mục");

            var page = new TaskDialogPage()
            {
                Caption = "Chọn dữ liệu",
                Heading = "Bạn muốn chọn loại dữ liệu nào?",
                Buttons = new TaskDialogButtonCollection() { btnFile, btnFolder, TaskDialogButton.Cancel }
            };

            var result = TaskDialog.ShowDialog(this, page);

            if (result == btnFile)
            {
                using var fileDialog = new OpenFileDialog();
                fileDialog.Title = "Chọn file";
                fileDialog.Filter = "Tất cả file (*.*)|*.*";
                if (fileDialog.ShowDialog(this) == DialogResult.OK)
                    SetSelectedPath(fileDialog.FileName);
            }
            else if (result == btnFolder)
            {
                using var folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "Chọn thư mục";
                if (folderDialog.ShowDialog(this) == DialogResult.OK)
                    SetSelectedPath(folderDialog.SelectedPath);
            }
        }

        private void SetSelectedPath(string path)
        {
            _selectedPath = path;
            lblSelectedPath.Text = Path.GetFileName(path);
            if (string.IsNullOrEmpty(lblSelectedPath.Text)) lblSelectedPath.Text = path;
            lblSelectedPath.Visible = true;
            lblDragDrop.Visible = false;

            bool isFolder = Directory.Exists(path);
            ApplyMode(encrypt: !path.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) || isFolder);

            if (_isEncryptMode)
            {
                lblIcon.Text = isFolder ? "" : ""; // Folder / File icon
                lblIcon.ForeColor = EncryptIconColor;
            }
            else
            {
                lblIcon.Text = ""; // Lock icon
                lblIcon.ForeColor = DecryptIconColor;
            }
            lblIcon.Visible = true;
        }

        // ============================================
        // TRẠNG THÁI FORM (một nơi duy nhất quản lý)
        // ============================================

        /// <summary>Chuyển giao diện giữa chế độ mã hóa và giải mã; luôn reset chế độ mã khôi phục.</summary>
        private void ApplyMode(bool encrypt)
        {
            _isEncryptMode = encrypt;

            if (encrypt)
            {
                btnAction.Text = "MÃ HÓA DỮ LIỆU";
                btnAction.FillColor = Color.FromArgb(74, 110, 255);
                btnAction.FillColor2 = Color.FromArgb(148, 94, 255);
            }
            else
            {
                btnAction.Text = "GIẢI MÃ DỮ LIỆU";
                btnAction.FillColor = Color.FromArgb(46, 204, 113);
                btnAction.FillColor2 = Color.FromArgb(26, 188, 156);
            }

            txtConfirmPassword.Visible = encrypt;
            chkDeleteOriginal.Visible = encrypt;
            lblForgotPassword.Visible = !encrypt;

            SetRecoveryMode(false);
            SetMasked(txtConfirmPassword, true);
        }

        private void SetRecoveryMode(bool recovery)
        {
            _isRecoveryMode = recovery;
            if (recovery)
            {
                lblForgotPassword.Text = "Đã nhớ mật khẩu? Dùng mật khẩu";
                txtPassword.PlaceholderText = "Nhập mã khôi phục (FL-XXXX-XXXX-XXXX)";
                txtPassword.UseSystemPasswordChar = false;
                txtPassword.PasswordChar = '\0';
                txtPassword.IconRight = null;
            }
            else
            {
                lblForgotPassword.Text = "Quên mật khẩu? Dùng mã khôi phục";
                txtPassword.PlaceholderText = "Mật khẩu";
                SetMasked(txtPassword, true);
            }
        }

        private static void SetMasked(Guna.UI2.WinForms.Guna2TextBox box, bool masked)
        {
            box.UseSystemPasswordChar = masked;
            box.PasswordChar = masked ? MaskChar : '\0';
            box.IconRight = masked ? EyeClosedIcon : EyeOpenIcon;
        }

        private static bool IsMasked(Guna.UI2.WinForms.Guna2TextBox box) =>
            box.PasswordChar == MaskChar || box.UseSystemPasswordChar;

        /// <summary>Khóa/mở toàn bộ input trong lúc đang xử lý.</summary>
        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            pnlDragDrop.AllowDrop = !busy;
            pnlDragDrop.Enabled = !busy;
            txtPassword.Enabled = !busy;
            txtConfirmPassword.Enabled = !busy;
            chkDeleteOriginal.Enabled = !busy;
            lblForgotPassword.Enabled = !busy;
            btnAction.Enabled = !busy;
            btnClose.Enabled = !busy;
            UseWaitCursor = busy;
            if (!busy) progressBar.Visible = false;
        }

        private void ResetForm()
        {
            _selectedPath = "";
            lblSelectedPath.Text = "";
            lblSelectedPath.Visible = false;
            lblIcon.Visible = false;
            lblDragDrop.Visible = true;
            lblDragDrop.Text = "Kéo & Thả File/Folder vào đây\nHoặc click để chọn";
            txtPassword.Clear();
            txtConfirmPassword.Clear();
            ApplyMode(encrypt: true);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isBusy && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MessageBox.Show(this, "Đang xử lý dữ liệu, vui lòng đợi hoàn tất trước khi đóng.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lblForgotPassword_Click(object? sender, EventArgs e)
        {
            if (_isBusy) return;
            SetRecoveryMode(!_isRecoveryMode);
        }

        private void txtPassword_IconRightClick(object? sender, EventArgs e)
        {
            if (_isRecoveryMode) return;
            SetMasked(txtPassword, !IsMasked(txtPassword));
        }

        private void txtConfirmPassword_IconRightClick(object? sender, EventArgs e)
        {
            SetMasked(txtConfirmPassword, !IsMasked(txtConfirmPassword));
        }

        private static Bitmap DrawEyeIcon(bool open)
        {
            var bmp = new Bitmap(24, 24);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.Gray, 2);
            g.DrawCurve(pen, new[] { new Point(2, 12), new Point(12, 4), new Point(22, 12) });
            g.DrawCurve(pen, new[] { new Point(2, 12), new Point(12, 20), new Point(22, 12) });

            if (open)
                g.DrawEllipse(pen, 9, 9, 6, 6);
            else
                g.DrawLine(pen, 4, 4, 20, 20);
            return bmp;
        }

        // ============================================
        // HÀNH ĐỘNG CHÍNH
        // ============================================

        private async void btnAction_Click(object? sender, EventArgs e)
        {
            if (_isBusy) return;

            if (string.IsNullOrEmpty(_selectedPath))
            {
                MessageBox.Show(this, "Vui lòng chọn hoặc kéo thả File/Folder!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show(this, "Vui lòng nhập " + (_isRecoveryMode ? "mã khôi phục" : "mật khẩu") + "!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isEncryptMode && txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show(this, "Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
                MessageBox.Show(this, "Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Progress<int> StartProgress()
        {
            SetBusy(true);
            progressBar.Value = 0;
            progressBar.Visible = true;
            return new Progress<int>(percent => progressBar.Value = Math.Clamp(percent, 0, 100));
        }

        private async Task EncryptData()
        {
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Lưu file mã hóa";
            saveDialog.Filter = "Encrypted file (*.enc)|*.enc";
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
                MessageBox.Show(this, result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowSuccessWithCopy(result.Message, result.RecoveryCode ?? "");

            // Chỉ xóa bản gốc sau khi người dùng đã thấy mã khôi phục
            if (deleteSrc)
            {
                var del = _controller.DeleteSource(source);
                if (!del.Success)
                    MessageBox.Show(this, del.Message + "\nFile mã hóa vẫn đã được tạo: " + dest, "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ResetForm();
        }

        private async Task DecryptData()
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn nơi xuất file/folder sau khi giải mã";

            if (folderDialog.ShowDialog(this) != DialogResult.OK) return;

            string outFolder = folderDialog.SelectedPath;
            string source = _selectedPath;
            string pass = txtPassword.Text;

            var result = await RunDecrypt(source, outFolder, pass, checkOverwrite: true);

            if (result.NeedsOverwriteConfirm)
            {
                var confirm = MessageBox.Show(this, $"File/Folder '{result.OutputPath}' đã tồn tại.\nBạn có muốn ghi đè không?",
                    "Xác nhận ghi đè", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                result = await RunDecrypt(source, outFolder, pass, checkOverwrite: false);
            }

            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, result.Message + "\n" + result.OutputPath, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void ShowSuccessWithCopy(string message, string recoveryCode)
        {
            using var f = new Form();
            f.Size = new Size(400, 230);
            f.StartPosition = FormStartPosition.CenterParent;
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = Color.White;
            f.ShowInTaskbar = false;

            using var components = new System.ComponentModel.Container();
            var borderless = new Guna.UI2.WinForms.Guna2BorderlessForm(components);
            borderless.ContainerControl = f;
            borderless.BorderRadius = 15;

            var lblTitle = new Label() { Text = message, Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold), ForeColor = Color.FromArgb(46, 204, 113) };
            var lblSub = new Label() { Text = "Yêu cầu lưu trữ bảo mật Mã khôi phục hệ thống dưới đây:", Location = new Point(20, 60), AutoSize = true, Font = new Font("Segoe UI", 10), ForeColor = Color.Gray };

            var txt = new Guna.UI2.WinForms.Guna2TextBox()
            {
                Text = recoveryCode,
                Location = new Point(20, 95),
                Size = new Size(360, 45),
                ReadOnly = true,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                BorderRadius = 8,
                TextAlign = HorizontalAlignment.Center,
                ForeColor = Color.FromArgb(50, 50, 50)
            };

            var btnCopy = new Guna.UI2.WinForms.Guna2Button() { Text = "Copy Mã", Location = new Point(20, 160), Size = new Size(150, 45), BorderRadius = 8, FillColor = Color.FromArgb(74, 110, 255), Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold) };
            var btnOk = new Guna.UI2.WinForms.Guna2Button() { Text = "XONG", Location = new Point(230, 160), Size = new Size(150, 45), BorderRadius = 8, FillColor = Color.FromArgb(230, 230, 230), ForeColor = Color.FromArgb(100, 100, 100), Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold) };

            btnCopy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(recoveryCode);
                    btnCopy.Text = "Đã Copy!";
                    btnCopy.FillColor = Color.FromArgb(46, 204, 113);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // Clipboard đang bị chương trình khác giữ
                    btnCopy.Text = "Thử lại";
                    MessageBox.Show(f, "Không truy cập được clipboard. Hãy thử lại hoặc chép mã thủ công.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnOk.Click += (s, e) => f.Close();

            f.Controls.Add(lblTitle);
            f.Controls.Add(lblSub);
            f.Controls.Add(txt);
            f.Controls.Add(btnCopy);
            f.Controls.Add(btnOk);
            f.AcceptButton = btnOk;

            f.Load += (s, e) =>
            {
                f.ActiveControl = btnCopy;
                txt.SelectionStart = 0;
                txt.SelectionLength = 0;
            };

            f.ShowDialog(this);
        }
    }
}
