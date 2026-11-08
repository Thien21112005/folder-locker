using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FolderLocker.Models;
using FolderLocker.Views.Theming;
using Guna.UI2.WinForms;

namespace FolderLocker.Views
{
    /// <summary>Thu thập mật khẩu hiện tại + mật khẩu mới. Không tự thực hiện thao tác.</summary>
    internal sealed class ChangePasswordDialog : Form
    {
        private const char Mask = '●';
        private const int ContentWidth = 400;

        private readonly Label _heading = new();
        private readonly Label _note = new();
        private readonly Label _lblCurrent = new();
        private readonly Guna2TextBox _txtCurrent = new();
        private readonly Label _lblNew = new();
        private readonly Guna2TextBox _txtNew = new();
        private readonly Label _lblStrength = new();
        private readonly Label _lblConfirm = new();
        private readonly Guna2TextBox _txtConfirm = new();
        private readonly Label _lblMismatch = new();
        private readonly Guna2CheckBox _chkNewCode = new();
        private readonly Guna2Button _btnOk = new();
        private readonly Guna2Button _btnCancel = new();

        public string CurrentSecret => _txtCurrent.Text;
        public string NewPassword => _txtNew.Text;
        public bool NewRecoveryCode => _chkNewCode.Checked;

        public ChangePasswordDialog(string encPath, string prefillCurrent)
        {
            Text = "Đổi mật khẩu";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Font = Theme.Body;

            _heading.AutoSize = true;
            _heading.Font = Theme.Heading;
            _heading.Text = "Đổi mật khẩu cho " + Path.GetFileName(encPath);
            _heading.MaximumSize = new Size(ContentWidth, 0);
            _heading.Margin = new Padding(0, 0, 0, 6);

            _note.AutoSize = true;
            _note.Font = Theme.Small;
            _note.MaximumSize = new Size(ContentWidth, 0);
            _note.Margin = new Padding(0, 0, 0, 14);
            _note.Text = "Dữ liệu không bị mã hóa lại nên đổi rất nhanh. Lưu ý: bản sao cũ của file (nếu có) vẫn mở được bằng mật khẩu cũ. " +
                         "Nếu mật khẩu cũ đã bị lộ, hãy giải mã rồi mã hóa lại.";

            Field(_lblCurrent, "Mật khẩu hoặc mã khôi phục hiện tại", _txtCurrent);
            Field(_lblNew, "Mật khẩu mới", _txtNew, $"Tối thiểu {PasswordStrength.MinLength} ký tự");
            Field(_lblConfirm, "Xác nhận mật khẩu mới", _txtConfirm, "Nhập lại mật khẩu mới");
            _txtCurrent.Text = prefillCurrent;
            _txtNew.Margin = new Padding(0, 0, 0, 2);
            _txtConfirm.Margin = new Padding(0, 0, 0, 2);

            foreach (var hint in new[] { _lblStrength, _lblMismatch })
            {
                hint.AutoSize = false;
                hint.Size = new Size(ContentWidth, 18);
                hint.Font = Theme.Small;
                hint.Margin = new Padding(0, 0, 0, 8);
            }

            _chkNewCode.AutoSize = true;
            _chkNewCode.Font = Theme.Small;
            _chkNewCode.Text = "Tạo mã khôi phục mới (mã cũ sẽ hết hiệu lực)";
            _chkNewCode.Margin = new Padding(0, 0, 0, 18);
            _chkNewCode.CheckedState.BorderRadius = 2;
            _chkNewCode.UncheckedState.BorderRadius = 2;
            _chkNewCode.UncheckedState.BorderThickness = 1;

            _btnOk.Text = "Đổi mật khẩu";
            _btnCancel.Text = "Hủy";
            foreach (var b in new[] { _btnOk, _btnCancel })
            {
                b.Font = Theme.BodyStrong;
                b.BorderRadius = 8;
                b.Size = new Size(130, 40);
                b.Margin = new Padding(8, 0, 0, 0);
                b.AccessibleName = b.Text;
            }
            _btnOk.Click += (_, _) => Submit();
            _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Width = ContentWidth,
                Margin = new Padding(0),
                Anchor = AnchorStyles.Right,
            };
            buttons.Controls.Add(_btnOk);
            buttons.Controls.Add(_btnCancel);

            var root = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(24, 20, 24, 20),
                Dock = DockStyle.Fill,
            };
            root.Controls.AddRange(new Control[]
            {
                _heading, _note, _lblCurrent, _txtCurrent, _lblNew, _txtNew, _lblStrength,
                _lblConfirm, _txtConfirm, _lblMismatch, _chkNewCode, buttons,
            });
            Controls.Add(root);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
            _txtNew.TextChanged += (_, _) => UpdateHints();
            _txtConfirm.TextChanged += (_, _) => UpdateHints();

            ApplyTheme(Theme.Current);
            UpdateHints();
        }

        private static void Field(Label label, string text, Guna2TextBox box, string? placeholder = null)
        {
            label.AutoSize = true;
            label.Font = Theme.Small;
            label.Text = text;
            label.Margin = new Padding(0, 0, 0, 4);
            box.Size = new Size(ContentWidth, 40);
            box.BorderRadius = 8;
            box.Font = Theme.Body;
            box.PasswordChar = Mask;
            box.UseSystemPasswordChar = true;
            box.PlaceholderText = placeholder ?? "";
            box.AccessibleName = text;
            box.Margin = new Padding(0, 0, 0, 12);
        }

        private void ApplyTheme(Palette p)
        {
            BackColor = p.Background;
            ForeColor = p.TextPrimary;
            _heading.ForeColor = p.TextPrimary;
            _note.ForeColor = p.TextSecondary;
            foreach (var l in new[] { _lblCurrent, _lblNew, _lblConfirm }) l.ForeColor = p.TextPrimary;
            foreach (var b in new[] { _txtCurrent, _txtNew, _txtConfirm }) ThemeStyles.TextBox(b, p);
            ThemeStyles.CheckBox(_chkNewCode, p);
            ThemeStyles.PrimaryButton(_btnOk, p);
            ThemeStyles.SecondaryButton(_btnCancel, p);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle, Theme.Current);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            (_txtCurrent.Text.Length > 0 ? _txtNew : _txtCurrent).Focus();
        }

        private void UpdateHints()
        {
            var p = Theme.Current;
            var level = PasswordStrength.Evaluate(_txtNew.Text);
            _lblStrength.Text = level == StrengthLevel.Empty ? "" : "Độ mạnh: " + PasswordStrength.Describe(level);
            _lblStrength.ForeColor = level switch
            {
                StrengthLevel.TooShort or StrengthLevel.Weak => p.Danger,
                StrengthLevel.Fair => p.Warning,
                _ => p.Success,
            };

            if (_txtConfirm.Text.Length == 0) _lblMismatch.Text = "";
            else if (_txtConfirm.Text == _txtNew.Text) { _lblMismatch.Text = "✓ Mật khẩu đã khớp"; _lblMismatch.ForeColor = p.Success; }
            else { _lblMismatch.Text = "Mật khẩu xác nhận chưa khớp"; _lblMismatch.ForeColor = p.Danger; }
        }

        private void Submit()
        {
            if (_txtCurrent.Text.Length == 0)
            {
                Dialogs.Warn(this, "Chưa nhập mật khẩu hoặc mã khôi phục hiện tại");
                _txtCurrent.Focus();
                return;
            }

            var level = PasswordStrength.Evaluate(_txtNew.Text);
            if (level is StrengthLevel.Empty or StrengthLevel.TooShort)
            {
                Dialogs.Warn(this, "Mật khẩu mới quá ngắn", $"Cần tối thiểu {PasswordStrength.MinLength} ký tự.");
                _txtNew.Focus();
                return;
            }

            if (_txtNew.Text != _txtConfirm.Text)
            {
                Dialogs.Warn(this, "Mật khẩu xác nhận chưa khớp");
                _txtConfirm.Focus();
                return;
            }

            if (level == StrengthLevel.Weak &&
                !Dialogs.Confirm(this, "Mật khẩu mới yếu", "Mật khẩu này dễ bị đoán. Vẫn dùng?", "Vẫn dùng", "Đổi mật khẩu khác", dangerous: true))
            {
                _txtNew.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
