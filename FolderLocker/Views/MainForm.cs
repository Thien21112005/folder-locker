using FolderLocker.Controllers;

namespace FolderLocker.Views
{
    public partial class MainForm : Form
    {
        private readonly LockerController _controller = new LockerController();
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnChoose_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn folder cần mã hóa (hoặc Cancel để chọn file)";

            if(folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = folderDialog.SelectedPath;
                return;
            }
            // Nếu cancel thì chọn file 
            using var fileDialog = new OpenFileDialog();
            fileDialog.Title = "Chọn file cần mã hóa";
            fileDialog.Filter = "Tất cả file (*.*)|*.*";
            if (fileDialog.ShowDialog() == DialogResult.OK)
                txtPath.Text = fileDialog.FileName;
        }

        //Mã hóa 
        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            // Chọn nơi lưu file .enc
            using var saveDialog = new SaveFileDialog();
            saveDialog.Title = "Lưu file mã hóa";
            saveDialog.Filter = "Encrypted file (*.enc)|*.enc";
            saveDialog.FileName = Path.GetFileName(txtPath.Text) + ".enc";

            if (saveDialog.ShowDialog() != DialogResult.OK) return;

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

            txtPath.Clear();
            txtPassword.Clear();

        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            // Chọn file .enc
            using var openDialog = new OpenFileDialog();
            openDialog.Title = "Chọn file cần giải mã";
            openDialog.Filter = "Encrypted file (*.enc)|*.enc";

            if (openDialog.ShowDialog() != DialogResult.OK) return;

            // Chọn nơi xuất
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Chọn nơi xuất file/folder sau khi giải mã";

            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            // Gọi Controller giải mã
            var result = _controller.Decrypt(openDialog.FileName, folderDialog.SelectedPath, txtPassword.Text);

            MessageBox.Show(result.message,
                result.success ? "Thành công" : "Thất bại",
                MessageBoxButtons.OK,
                result.success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            if (result.success)
            {
                txtPath.Clear();
                txtPassword.Clear();
            }
        }
    }
}
