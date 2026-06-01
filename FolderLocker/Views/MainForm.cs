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
            txtPassword.IconRight = DrawEyeIcon(false);
            txtConfirmPassword.IconRight = DrawEyeIcon(false);
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
                btnAction.FillColor = System.Drawing.Color.FromArgb(74, 110, 255);
                btnAction.FillColor2 = System.Drawing.Color.FromArgb(148, 94, 255);
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
                btnAction.FillColor = System.Drawing.Color.FromArgb(46, 204, 113);
                btnAction.FillColor2 = System.Drawing.Color.FromArgb(26, 188, 156);
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
                txtPassword.PasswordChar = '\0';
                txtPassword.IconRight = null;
            }
            else
            {
                lblForgotPassword.Text = "Quên mật khẩu? Dùng mã khôi phục";
                txtPassword.PlaceholderText = "Mật khẩu";
                txtPassword.UseSystemPasswordChar = true;
                txtPassword.PasswordChar = '●';
                txtPassword.IconRight = DrawEyeIcon(false);
            }
        }

        private System.Drawing.Bitmap DrawEyeIcon(bool open)
        {
            var bmp = new System.Drawing.Bitmap(24, 24);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new System.Drawing.Pen(System.Drawing.Color.Gray, 2);
            g.DrawCurve(pen, new System.Drawing.Point[] { new System.Drawing.Point(2, 12), new System.Drawing.Point(12, 4), new System.Drawing.Point(22, 12) });
            g.DrawCurve(pen, new System.Drawing.Point[] { new System.Drawing.Point(2, 12), new System.Drawing.Point(12, 20), new System.Drawing.Point(22, 12) });
            
            if (open)
            {
                g.DrawEllipse(pen, 9, 9, 6, 6);
            }
            else
            {
                g.DrawLine(pen, 4, 4, 20, 20);
            }
            return bmp;
        }

        private void txtPassword_IconRightClick(object sender, EventArgs e)
        {
            if (_isRecoveryMode) return;
            bool isHidden = txtPassword.PasswordChar == '●' || txtPassword.UseSystemPasswordChar;
            
            if (isHidden)
            {
                txtPassword.UseSystemPasswordChar = false;
                txtPassword.PasswordChar = '\0';
            }
            else
            {
                txtPassword.UseSystemPasswordChar = true;
                txtPassword.PasswordChar = '●';
            }
            txtPassword.IconRight = DrawEyeIcon(isHidden);
        }

        private void txtConfirmPassword_IconRightClick(object sender, EventArgs e)
        {
            bool isHidden = txtConfirmPassword.PasswordChar == '●' || txtConfirmPassword.UseSystemPasswordChar;
            
            if (isHidden)
            {
                txtConfirmPassword.UseSystemPasswordChar = false;
                txtConfirmPassword.PasswordChar = '\0';
            }
            else
            {
                txtConfirmPassword.UseSystemPasswordChar = true;
                txtConfirmPassword.PasswordChar = '●';
            }
            txtConfirmPassword.IconRight = DrawEyeIcon(isHidden);
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

            ShowSuccessWithCopy(result.message, result.recoveryCode);

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
            btnAction.FillColor = System.Drawing.Color.FromArgb(74, 110, 255);
            btnAction.FillColor2 = System.Drawing.Color.FromArgb(148, 94, 255);
            txtConfirmPassword.Visible = true;
            chkDeleteOriginal.Visible = true;
            lblForgotPassword.Visible = false;
            _isRecoveryMode = false;
            txtPassword.PlaceholderText = "Mật khẩu";
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.PasswordChar = '●';
            txtPassword.IconRight = DrawEyeIcon(false);
            
            txtConfirmPassword.UseSystemPasswordChar = true;
            txtConfirmPassword.PasswordChar = '●';
            txtConfirmPassword.IconRight = DrawEyeIcon(false);
        }

        private void ShowSuccessWithCopy(string message, string recoveryCode)
        {
            using var f = new Form();
            f.Size = new System.Drawing.Size(400, 230);
            f.StartPosition = FormStartPosition.CenterParent;
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = System.Drawing.Color.White;

            var components = new System.ComponentModel.Container();
            var borderless = new Guna.UI2.WinForms.Guna2BorderlessForm(components);
            borderless.ContainerControl = f;
            borderless.BorderRadius = 15;

            var lblTitle = new System.Windows.Forms.Label() { Text = "Thành công!", Location = new System.Drawing.Point(20, 20), AutoSize = true, Font = new System.Drawing.Font("Segoe UI Semibold", 16, System.Drawing.FontStyle.Bold), ForeColor = System.Drawing.Color.FromArgb(46, 204, 113) };
            var lblSub = new System.Windows.Forms.Label() { Text = "Yêu cầu lưu trữ bảo mật Mã khôi phục hệ thống dưới đây:", Location = new System.Drawing.Point(20, 60), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 10), ForeColor = System.Drawing.Color.Gray };

            var txt = new Guna.UI2.WinForms.Guna2TextBox() { 
                Text = recoveryCode, 
                Location = new System.Drawing.Point(20, 95), 
                Size = new System.Drawing.Size(360, 45), 
                ReadOnly = true, 
                Font = new System.Drawing.Font("Segoe UI", 13, System.Drawing.FontStyle.Bold),
                BorderRadius = 8,
                TextAlign = HorizontalAlignment.Center,
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 50)
            };

            var btnCopy = new Guna.UI2.WinForms.Guna2Button() { Text = "Copy Mã", Location = new System.Drawing.Point(20, 160), Size = new System.Drawing.Size(150, 45), BorderRadius = 8, FillColor = System.Drawing.Color.FromArgb(74, 110, 255), Font = new System.Drawing.Font("Segoe UI Semibold", 10, System.Drawing.FontStyle.Bold) };
            var btnOk = new Guna.UI2.WinForms.Guna2Button() { Text = "XONG", Location = new System.Drawing.Point(230, 160), Size = new System.Drawing.Size(150, 45), BorderRadius = 8, FillColor = System.Drawing.Color.FromArgb(230, 230, 230), ForeColor = System.Drawing.Color.FromArgb(100, 100, 100), Font = new System.Drawing.Font("Segoe UI Semibold", 10, System.Drawing.FontStyle.Bold) };
            
            btnCopy.Click += (s, e) => {
                Clipboard.SetText(recoveryCode);
                btnCopy.Text = "Đã Copy!";
                btnCopy.FillColor = System.Drawing.Color.FromArgb(46, 204, 113);
            };

            btnOk.Click += (s, e) => f.Close();

            f.Controls.Add(lblTitle);
            f.Controls.Add(lblSub);
            f.Controls.Add(txt);
            f.Controls.Add(btnCopy);
            f.Controls.Add(btnOk);
            f.AcceptButton = btnOk;

            f.Load += (s, e) => {
                f.ActiveControl = btnCopy;
                txt.SelectionStart = 0;
                txt.SelectionLength = 0;
            };
            
            f.ShowDialog(this);
            components.Dispose();
        }
    }
}