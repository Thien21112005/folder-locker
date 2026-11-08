using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FolderLocker.Views.Theming;
using Guna.UI2.WinForms;

namespace FolderLocker.Views
{
    /// <summary>Một file .enc và mã khôi phục của nó.</summary>
    internal sealed record RecoveryEntry(string EncPath, string Code);

    /// <summary>
    /// Hiển thị mã khôi phục (chỉ một lần). Không cho đóng "lỡ tay": nút Xong chỉ bật khi đã
    /// sao chép, đã lưu ra file, hoặc đã xác nhận lưu ở nơi khác; Esc/Alt+F4 sẽ hỏi lại.
    /// </summary>
    internal sealed class RecoveryCodeDialog : Form
    {
        private readonly IReadOnlyList<RecoveryEntry> _entries;
        private readonly Label _heading = new();
        private readonly Label _body = new();
        private readonly Label _warning = new();
        private readonly Guna2TextBox _codeBox = new();
        private readonly Guna2CheckBox _chkSaved = new();
        private readonly Guna2Button _btnCopy = new();
        private readonly Guna2Button _btnSave = new();
        private readonly Guna2Button _btnDone = new();
        private readonly ToolTip _toolTip = new();
        private bool _copied;
        private bool _saved;

        public static void ShowFor(IWin32Window owner, IReadOnlyList<RecoveryEntry> entries)
        {
            if (entries.Count == 0) return;
            using var dlg = new RecoveryCodeDialog(entries);
            dlg.ShowDialog(owner);
        }

        private RecoveryCodeDialog(IReadOnlyList<RecoveryEntry> entries)
        {
            _entries = entries;
            bool single = entries.Count == 1;

            Text = "Mã khôi phục";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            KeyPreview = true;
            Font = Theme.Body;

            const int contentWidth = 440;

            _heading.AutoSize = true;
            _heading.Font = Theme.Heading;
            _heading.Margin = new Padding(0, 0, 0, 8);
            _heading.Text = single ? "Đã mã hóa xong" : $"Đã mã hóa xong {entries.Count} mục";

            _body.AutoSize = true;
            _body.MaximumSize = new Size(contentWidth, 0);
            _body.Margin = new Padding(0, 0, 0, 14);
            _body.Text = (single ? "Đây là mã khôi phục của " + Path.GetFileName(entries[0].EncPath) : "Đây là mã khôi phục của từng file")
                         + ". Mã mở được file khi bạn quên mật khẩu và chỉ hiển thị MỘT LẦN. Hãy sao chép hoặc lưu lại ngay.";

            _codeBox.ReadOnly = true;
            _codeBox.BorderRadius = 8;
            _codeBox.Margin = new Padding(0, 0, 0, 10);
            _codeBox.AccessibleName = single ? "Mã khôi phục" : "Danh sách mã khôi phục";
            if (single)
            {
                _codeBox.Text = entries[0].Code;
                _codeBox.Font = Theme.Code;
                _codeBox.TextAlign = HorizontalAlignment.Center;
                _codeBox.Size = new Size(contentWidth, 56);
            }
            else
            {
                _codeBox.Multiline = true;
                _codeBox.ScrollBars = ScrollBars.Vertical;
                _codeBox.Font = new Font("Consolas", 11F);
                _codeBox.Text = string.Join(Environment.NewLine, entries.Select(e => $"{e.Code}   {Path.GetFileName(e.EncPath)}"));
                _codeBox.Size = new Size(contentWidth, Math.Min(200, 30 + entries.Count * 22));
            }

            _warning.AutoSize = true;
            _warning.MaximumSize = new Size(contentWidth, 0);
            _warning.Font = Theme.Small;
            _warning.Margin = new Padding(0, 0, 0, 12);
            _warning.Text = "⚠ Ai có mã này đều mở được file. Đừng lưu mã chung chỗ với file .enc.";

            _chkSaved.AutoSize = true;
            _chkSaved.Font = Theme.Small;
            _chkSaved.Margin = new Padding(0, 0, 0, 18);
            _chkSaved.Text = "Tôi đã ghi lại mã ở nơi an toàn khác";
            _chkSaved.CheckedState.BorderRadius = 2;
            _chkSaved.UncheckedState.BorderRadius = 2;
            _chkSaved.UncheckedState.BorderThickness = 1;
            _chkSaved.CheckedChanged += (_, _) => UpdateDone();

            ConfigureButton(_btnCopy, single ? "Sao chép mã" : "Sao chép tất cả", 150);
            ConfigureButton(_btnSave, "Lưu ra file .txt…", 150);
            ConfigureButton(_btnDone, "Xong", 110);
            _btnCopy.Click += (_, _) => Copy();
            _btnSave.Click += (_, _) => SaveToFile();
            _btnDone.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

            var buttons = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Width = contentWidth,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
            };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttons.Controls.Add(_btnCopy, 0, 0);
            buttons.Controls.Add(_btnSave, 1, 0);
            buttons.Controls.Add(_btnDone, 3, 0);

            var root = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(24, 20, 24, 20),
                Dock = DockStyle.Fill,
            };
            root.Controls.Add(_heading);
            root.Controls.Add(_body);
            root.Controls.Add(_codeBox);
            root.Controls.Add(_warning);
            root.Controls.Add(_chkSaved);
            root.Controls.Add(buttons);
            Controls.Add(root);

            AcceptButton = _btnDone;
            _btnCopy.TabIndex = 0;
            _btnSave.TabIndex = 1;
            _chkSaved.TabIndex = 2;
            _btnDone.TabIndex = 3;

            ApplyTheme(Theme.Current);
            UpdateDone();
        }

        private static void ConfigureButton(Guna2Button btn, string text, int width)
        {
            btn.Text = text;
            btn.AccessibleName = text;
            btn.Font = Theme.BodyStrong;
            btn.BorderRadius = 8;
            btn.Size = new Size(width, 40);
            btn.Margin = new Padding(0, 0, 8, 0);
        }

        private void ApplyTheme(Palette p)
        {
            BackColor = p.Background;
            ForeColor = p.TextPrimary;
            _heading.ForeColor = p.Success;
            _body.ForeColor = p.TextPrimary;
            _warning.ForeColor = p.Warning;
            ThemeStyles.TextBox(_codeBox, p);
            _codeBox.FillColor = p.Surface;
            ThemeStyles.CheckBox(_chkSaved, p);
            ThemeStyles.SecondaryButton(_btnCopy, p);
            ThemeStyles.SecondaryButton(_btnSave, p);
            ThemeStyles.PrimaryButton(_btnDone, p);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle, Theme.Current);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _btnCopy.Focus();
        }

        private void UpdateDone()
        {
            bool ok = _copied || _saved || _chkSaved.Checked;
            _btnDone.Enabled = ok;
            _toolTip.SetToolTip(_btnDone, ok ? "" : "Hãy sao chép, lưu ra file, hoặc tích ô xác nhận trước");
        }

        private string AllCodesText() =>
            _entries.Count == 1
                ? _entries[0].Code
                : string.Join(Environment.NewLine, _entries.Select(e => $"{Path.GetFileName(e.EncPath)}: {e.Code}"));

        private void Copy()
        {
            try
            {
                Clipboard.SetText(AllCodesText());
                _copied = true;
                _btnCopy.Text = "✓ Đã sao chép";
                UpdateDone();
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                Dialogs.Warn(this, "Không truy cập được clipboard", "Clipboard đang bị chương trình khác giữ. Hãy thử lại hoặc lưu ra file.");
            }
        }

        private void SaveToFile()
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Lưu mã khôi phục",
                Filter = "Văn bản (*.txt)|*.txt",
                FileName = _entries.Count == 1
                    ? $"Ma khoi phuc - {Path.GetFileNameWithoutExtension(_entries[0].EncPath)}.txt"
                    : $"Ma khoi phuc - {DateTime.Now:yyyy-MM-dd HHmm}.txt",
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                File.WriteAllText(dlg.FileName, BuildFileContent(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Dialogs.Error(this, "Không lưu được file", ex.Message);
                return;
            }

            _saved = true;
            _btnSave.Text = "✓ Đã lưu";
            UpdateDone();

            string? savedDir = Path.GetDirectoryName(Path.GetFullPath(dlg.FileName));
            bool besideEnc = _entries.Any(e => string.Equals(Path.GetDirectoryName(Path.GetFullPath(e.EncPath)), savedDir, StringComparison.OrdinalIgnoreCase));
            if (besideEnc)
                Dialogs.Warn(this, "Mã đang nằm cùng thư mục với file .enc",
                    "Nếu ai đó lấy được cả thư mục, họ sẽ mở được file. Nên chuyển file mã sang nơi khác (USB, trình quản lý mật khẩu…).");
        }

        private string BuildFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("FOLDER LOCKER — MÃ KHÔI PHỤC");
            sb.AppendLine($"Tạo lúc: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
            foreach (var e in _entries)
            {
                sb.AppendLine($"File:  {Path.GetFileName(e.EncPath)}");
                sb.AppendLine($"Mã:    {e.Code}");
                sb.AppendLine();
            }
            sb.AppendLine("Dùng mã này thay cho mật khẩu khi giải mã nếu bạn quên mật khẩu.");
            sb.AppendLine("Ai có mã đều mở được file. Hãy cất file này ở nơi an toàn, tách biệt với file .enc.");
            return sb.ToString();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            bool acknowledged = _copied || _saved || _chkSaved.Checked;
            if (!acknowledged && e.CloseReason == CloseReason.UserClosing)
            {
                bool close = Dialogs.Confirm(this, "Bạn chưa lưu mã khôi phục",
                    "Sau khi đóng, mã sẽ không hiển thị lại. Nếu quên mật khẩu, bạn sẽ không thể mở file.",
                    "Vẫn đóng", "Quay lại lưu mã", dangerous: true);
                if (!close) e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
