using FolderLocker.Controllers;

namespace FolderLocker.Views
{
    public partial class MainForm : Form
    {
        private readonly LockerController _controller = new LockerController();

        public MainForm()
        {
            InitializeComponent();
            // Ẩn btnDecrypt mặc định
            btnDecrypt.Visible = false;
        }

        // ── Chọn file hoặc folder ──
        private void btnChoose_Click(object sender, EventArgs e)
        {
            // Thử chọn folder trước
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn folder/file (Cancel để chọn file)";

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = folderDialog.SelectedPath;
                UpdateButtons();
                return;
            }

            // Nếu Cancel thì chọn file
            using var fileDialog = new OpenFileDialog();
            fileDialog.Title = "Chọn file";
            fileDialog.Filter = "Tất cả file (*.*)|*.*";

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = fileDialog.FileName;
                UpdateButtons();
            }
        }

        // ── Ẩn hiện button tùy loại file ──
        private void UpdateButtons()
        {
            bool isEnc = txtPath.Text.EndsWith(".enc");
            btnEncrypt.Visible = !isEnc;  // không phải .enc → hiện Encrypt
            btnDecrypt.Visible = isEnc;   // là .enc → hiện Decrypt
        }

        // ── Mã hóa ──
        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            // Chọn nơi lưu file .enc
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Lưu file mã hóa";
            saveDialog.Filter = "Encrypted file (*.enc)|*.enc";
            saveDialog.FileName = Path.GetFileName(txtPath.Text) + ".enc";

            if (saveDialog.ShowDialog() != DialogResult.OK) return;

            // Gọi Controller mã hóa
            var result = _controller.Encrypt(txtPath.Text, saveDialog.FileName, txtPassword.Text);

            if (!result.success)
            {
                MessageBox.Show(result.message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Hỏi xóa file gốc không
            var confirm = MessageBox.Show(
                result.message + "\n\nXóa file/folder gốc luôn không?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                var deleteResult = _controller.DeleteSource(txtPath.Text);
                MessageBox.Show(deleteResult.message, "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            ClearForm();
        }

        // ── Giải mã ──
        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            // Chỉ hỏi nơi xuất thôi
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn nơi xuất file/folder sau khi giải mã";

            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            // Gọi Controller giải mã
            var result = _controller.Decrypt(txtPath.Text, folderDialog.SelectedPath, txtPassword.Text);

            MessageBox.Show(result.message,
                result.success ? "Thành công" : "Thất bại",
                MessageBoxButtons.OK,
                result.success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            if (result.success)
                ClearForm();
        }

        // ── Clear form sau khi xong ──
        private void ClearForm()
        {
            txtPath.Clear();
            txtPassword.Clear();
            btnEncrypt.Visible = true;
            btnDecrypt.Visible = false;
        }
    }
}