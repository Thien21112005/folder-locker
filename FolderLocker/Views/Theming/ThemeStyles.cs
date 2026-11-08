using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace FolderLocker.Views.Theming
{
    /// <summary>Áp token màu lên từng loại control. Một nơi duy nhất, không rải FromArgb.</summary>
    internal static class ThemeStyles
    {
        public static void TextBox(Guna2TextBox box, Palette p)
        {
            box.FillColor = p.InputFill;
            box.BorderColor = p.InputBorder;
            box.ForeColor = p.TextPrimary;
            box.PlaceholderForeColor = p.TextSecondary;
            box.HoverState.BorderColor = p.BorderStrong;
            box.FocusedState.BorderColor = p.Focus;
            box.DisabledState.FillColor = p.DisabledFill;
            box.DisabledState.BorderColor = p.Border;
            box.DisabledState.ForeColor = p.DisabledText;
            box.DisabledState.PlaceholderForeColor = p.DisabledText;
            box.BackColor = Color.Transparent;
        }

        public static void CheckBox(Guna2CheckBox chk, Palette p)
        {
            chk.ForeColor = p.TextPrimary;
            chk.BackColor = Color.Transparent;
            chk.CheckedState.FillColor = p.Focus;
            chk.CheckedState.BorderColor = p.Focus;
            chk.UncheckedState.BorderColor = p.BorderStrong;
            chk.UncheckedState.FillColor = p.InputFill;
        }

        /// <summary>Nút dạng liên kết: focus được bằng Tab, có trạng thái hover.</summary>
        public static void LinkButton(Guna2Button btn, Palette p)
        {
            btn.FillColor = Color.Transparent;
            btn.ForeColor = p.Link;
            btn.HoverState.FillColor = p.SurfaceHover;
            btn.HoverState.ForeColor = p.Link;
            btn.PressedColor = p.Border;
            btn.DisabledState.FillColor = Color.Transparent;
            btn.DisabledState.ForeColor = p.DisabledText;
            btn.FocusedColor = p.SurfaceHover;
            btn.BackColor = Color.Transparent;
        }

        /// <summary>Nút phụ (viền mảnh, nền surface).</summary>
        public static void SecondaryButton(Guna2Button btn, Palette p)
        {
            btn.FillColor = p.Surface;
            btn.ForeColor = p.TextPrimary;
            btn.BorderColor = p.BorderStrong;
            btn.BorderThickness = 1;
            btn.HoverState.FillColor = p.SurfaceHover;
            btn.HoverState.ForeColor = p.TextPrimary;
            btn.HoverState.BorderColor = p.Focus;
            btn.FocusedColor = p.SurfaceHover;
            btn.DisabledState.FillColor = p.DisabledFill;
            btn.DisabledState.ForeColor = p.DisabledText;
            btn.DisabledState.BorderColor = p.Border;
            btn.BackColor = Color.Transparent;
        }

        public static void PrimaryButton(Guna2Button btn, Palette p)
        {
            btn.FillColor = p.EncryptFrom;
            btn.ForeColor = p.TextOnAccent;
            btn.HoverState.FillColor = ControlPaint.Dark(p.EncryptFrom, 0.05f);
            btn.HoverState.ForeColor = p.TextOnAccent;
            btn.FocusedColor = p.EncryptFrom;
            btn.DisabledState.FillColor = p.DisabledFill;
            btn.DisabledState.ForeColor = p.DisabledText;
            btn.BackColor = Color.Transparent;
        }

        /// <summary>Nút chính ở trạng thái "Hủy": trung tính, không gây cảm giác nguy hiểm.</summary>
        public static void NeutralGradientButton(Guna2GradientButton btn, Palette p)
        {
            btn.FillColor = p.SurfaceHover;
            btn.FillColor2 = p.SurfaceHover;
            btn.ForeColor = p.TextPrimary;
            btn.BorderColor = p.BorderStrong;
            btn.BorderThickness = 1;
        }

        public static void GradientButton(Guna2GradientButton btn, Palette p, bool encrypt)
        {
            btn.BorderThickness = 0;
            btn.FillColor = encrypt ? p.EncryptFrom : p.DecryptFrom;
            btn.FillColor2 = encrypt ? p.EncryptTo : p.DecryptTo;
            btn.ForeColor = p.TextOnAccent;
            btn.DisabledState.FillColor = p.DisabledFill;
            btn.DisabledState.FillColor2 = p.DisabledFill;
            btn.DisabledState.ForeColor = p.DisabledText;
            btn.DisabledState.BorderColor = p.Border;
            btn.BackColor = Color.Transparent;
        }

        public static void ProgressBar(Guna2ProgressBar bar, Palette p, Color progress)
        {
            bar.FillColor = p.Track;
            bar.ProgressColor = progress;
            bar.ProgressColor2 = progress;
            bar.BackColor = Color.Transparent;
        }

        public static void ControlBox(Guna2ControlBox box, Palette p)
        {
            box.FillColor = Color.Transparent;
            box.IconColor = p.TextSecondary;
            box.HoverState.FillColor = p.SurfaceHover;
            box.HoverState.IconColor = p.TextPrimary;
            box.BackColor = Color.Transparent;
        }

        public static void Menu(ToolStrip menu, Palette p)
        {
            menu.Renderer = new ToolStripProfessionalRenderer(new MenuColors(p)) { RoundedEdges = false };
            menu.BackColor = p.Surface;
            menu.ForeColor = p.TextPrimary;
            foreach (ToolStripItem item in menu.Items)
                item.ForeColor = p.TextPrimary;
        }

        private sealed class MenuColors(Palette p) : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => p.Surface;
            public override Color MenuBorder => p.Border;
            public override Color MenuItemBorder => p.Focus;
            public override Color MenuItemSelected => p.SurfaceHover;
            public override Color MenuItemSelectedGradientBegin => p.SurfaceHover;
            public override Color MenuItemSelectedGradientEnd => p.SurfaceHover;
            public override Color ImageMarginGradientBegin => p.Surface;
            public override Color ImageMarginGradientMiddle => p.Surface;
            public override Color ImageMarginGradientEnd => p.Surface;
            public override Color SeparatorDark => p.Border;
            public override Color SeparatorLight => p.Surface;
            public override Color CheckBackground => p.SurfaceHover;
            public override Color CheckSelectedBackground => p.SurfaceHover;
            public override Color CheckPressedBackground => p.SurfaceHover;
        }
    }
}
