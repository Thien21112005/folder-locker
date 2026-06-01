using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FolderLocker.Controllers;

namespace FolderLocker.Views
{
    public partial class MainForm : Form
    {
        private readonly LockerController _controller = new LockerController();
        private string _selectedPath = "";
        private bool _isEncryptMode = true;
        private bool _isRecoveryMode = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void pnlDragDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void pnlDragDrop_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                SetSelectedPath(files[0]);
            }
        }

        private void pnlDragDrop_Click(object sender, EventArgs e)
        {
            var btnFile = new TaskDialogCommandLinkButton("Chọn File", "Mã hóa hoặc giải mã một tập tin đơn lẻ");
            var btnFolder = new TaskDialogCommandLinkButton("Chọn Thư mục (Folder)", "Mã hóa hoặc giải mã toàn bộ một thư mục");

            var page = new TaskDialogPage()
            {
                Caption = "Chọn dữ liệu",
                Heading = "Bạn muốn tải lên loại dữ liệu nào?",
                Buttons = new TaskDialogButtonCollection() { btnFile, btnFolder, TaskDialogButton.Cancel }
            };

            var result = TaskDialog.ShowDialog(this, page);

            if (result == btnFile)
            {
                using var fileDialog = new OpenFileDialog();
                fileDialog.Title = "Chọn file";
                fileDialog.Filter = "Tất cả file (*.*)|*.*";
                if (fileDialog.ShowDialog() == DialogResult.OK)
                    SetSelectedPath(fileDialog.FileName);
            }
            else if (result == btnFolder)
            {
                using var folderDialog = new FolderBrowserDialog();
                folderDialog.Description = "Chọn thư mục";
                if (folderDialog.ShowDialog() == DialogResult.OK)
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

            _isEncryptMode = !path.EndsWith(".enc", StringComparison.OrdinalIgnoreCase);
            
            if (_isEncryptMode)
            {
                if (Directory.Exists(path))
                {
                    lblIcon.Text = "\uE8B7"; // Folder icon
                    lblIcon.ForeColor = System.Drawing.Color.FromArgb(94, 148, 255);
                }
                else
                {
                    lblIcon.Text = "\uE7C3"; // File icon
                    lblIcon.ForeColor = System.Drawing.Color.FromArgb(94, 148, 255);
                }
                
                btnAction.Text = "MÃ HÓA DỮ LIỆU";
                btnAction.FillColor = System.Drawing.Color.FromArgb(94, 148, 255); // Blue
                txtConfirmPassword.Visible = true;
                chkDeleteOriginal.Visible = true;
                lblForgotPassword.Visible = false;
                _isRecoveryMode = false;
                txtPassword.PlaceholderText = "Mật khẩu";
            }
            else
            {
                lblIcon.Text = "\uE72E"; // Lock icon
                lblIcon.ForeColor = System.Drawing.Color.FromArgb(46, 204, 113); // Green

                btnAction.Text = "GIẢI MÃ DỮ LIỆU";
                btnAction.FillColor = System.Drawing.Color.FromArgb(46, 204, 113); // Green
                txtConfirmPassword.Visible = false;
                chkDeleteOriginal.Visible = false;
                lblForgotPassword.Visible = true;
            }
            lblIcon.Visible = true;
        }

        private void lblForgotPassword_Click(object sender, EventArgs e)
        {
            _isRecoveryMode = !_isRecoveryMode;
            if (_isRecoveryMode)
            {
                lblForgotPassword.Text = "Đã nhớ mật khẩu? Dùng mật khẩu";
                txtPassword.PlaceholderText = "Nhập mã khôi phục (FL-XXXX-XXXX-XXXX)";
                txtPassword.UseSystemPasswordChar = false;
            }
            else
            {
                lblForgotPassword.Text = "Quên mật khẩu? Dùng mã khôi phục";
                txtPassword.PlaceholderText = "Mật khẩu";
                txtPassword.UseSystemPasswordChar = true;
            }
        }

        private async void btnAction_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPath))
            {
                MessageBox.Show("Vui lòng chọn hoặc kéo thả File/Folder!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập " + (_isRecoveryMode ? "Mã khôi phục" : "Mật khẩu") + "!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isEncryptMode && txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnAction.Enabled = false;
            progressBar.Visible = true;
            progressBar.Value = 0;

            var progress = new Progress<int>(percent =>
            {
                if (percent >= 0 && percent <= 100)
                    progressBar.Value = percent;
            });

            if (_isEncryptMode)
            {
                await EncryptData(progress);
            }
            else
            {
                await DecryptData(progress);
            }

            btnAction.Enabled = true;
            progressBar.Visible = false;
        }

        private async Task EncryptData(IProgress<int> progress)
        {
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Lưu file mã hóa";
            saveDialog.Filter = "Encrypted file (*.enc)|*.enc";
            saveDialog.FileName = Path.GetFileName(_selectedPath) + ".enc";

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                progressBar.Visible = false;
                return;
            }

            string dest = saveDialog.FileName;
            string source = _selectedPath;
            string pass = txtPassword.Text;
            bool deleteSrc = chkDeleteOriginal.Checked;

            var result = await Task.Run(() => _controller.Encrypt(source, dest, pass, progress));

            if (!result.success)
            {
                MessageBox.Show(result.message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string msg = result.message + "\n\n⚠️ ĐÂY LÀ MÃ KHÔI PHỤC CỦA BẠN:\n" + result.recoveryCode + "\n\nHãy lưu lại cẩn thận phòng khi quên mật khẩu!";
            MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (deleteSrc)
            {
                _controller.DeleteSource(source);
            }

            ClearForm();
        }

        private async Task DecryptData(IProgress<int> progress)
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn nơi xuất file/folder sau khi giải mã";

            if (folderDialog.ShowDialog() != DialogResult.OK)
            {
                progressBar.Visible = false;
                return;
            }

            string outFolder = folderDialog.SelectedPath;
            string source = _selectedPath;
            string pass = txtPassword.Text;

            var result = await Task.Run(() => _controller.Decrypt(source, outFolder, pass, true, progress));

            if (!result.success)
            {
                if (result.message == "OVERWRITE_WARNING")
                {
                    var confirm = MessageBox.Show($"File/Folder '{result.outPath}' đã tồn tại.\nBạn có muốn ghi đè không?", "Xác nhận ghi đè", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm == DialogResult.Yes)
                    {
                        result = await Task.Run(() => _controller.Decrypt(source, outFolder, pass, false, progress));
                        if (!result.success)
                        {
                            MessageBox.Show(result.message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    MessageBox.Show(result.message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            MessageBox.Show(result.message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
        }

        private void ClearForm()
        {
            _selectedPath = "";
            lblSelectedPath.Text = "";
            lblSelectedPath.Visible = false;
            lblIcon.Visible = false;
            lblDragDrop.Visible = true;
            lblDragDrop.Text = "Kéo & Thả File/Folder vào đây\nHoặc click để chọn";
            txtPassword.Clear();
            txtConfirmPassword.Clear();
            
            _isEncryptMode = true;
            btnAction.Text = "MÃ HÓA DỮ LIỆU";
            btnAction.FillColor = System.Drawing.Color.FromArgb(94, 148, 255);
            txtConfirmPassword.Visible = true;
            chkDeleteOriginal.Visible = true;
            lblForgotPassword.Visible = false;
            _isRecoveryMode = false;
            txtPassword.PlaceholderText = "Mật khẩu";
            txtPassword.UseSystemPasswordChar = true;
        }
    }
}