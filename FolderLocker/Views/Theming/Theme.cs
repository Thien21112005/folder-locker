using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FolderLocker.Views.Theming
{
    /// <summary>
    /// Bộ token màu. Mọi cặp chữ/nền dùng cho nội dung đều đạt WCAG AA (≥ 4.5:1).
    /// Nút gradient luôn dùng chữ trắng trên nền đủ tối ở cả hai theme.
    /// </summary>
    internal sealed record Palette(
        bool IsDark,
        Color Background,
        Color Surface,
        Color SurfaceHover,
        Color Border,
        Color BorderStrong,
        Color TextPrimary,
        Color TextSecondary,
        Color TextOnAccent,
        Color Link,
        Color Focus,
        Color EncryptFrom,
        Color EncryptTo,
        Color DecryptFrom,
        Color DecryptTo,
        Color Success,
        Color Warning,
        Color Danger,
        Color InputFill,
        Color InputBorder,
        Color DisabledFill,
        Color DisabledText,
        Color DropTint,
        Color Track);

    internal static class Theme
    {
        public static readonly Palette Light = new(
            IsDark: false,
            Background: Color.White,
            Surface: Color.FromArgb(248, 249, 251),
            SurfaceHover: Color.FromArgb(241, 244, 249),
            Border: Color.FromArgb(214, 219, 226),
            BorderStrong: Color.FromArgb(150, 158, 170),
            TextPrimary: Color.FromArgb(31, 35, 40),
            TextSecondary: Color.FromArgb(87, 96, 106),
            TextOnAccent: Color.White,
            Link: Color.FromArgb(37, 99, 235),
            Focus: Color.FromArgb(37, 99, 235),
            EncryptFrom: Color.FromArgb(59, 91, 219),
            EncryptTo: Color.FromArgb(124, 77, 230),
            DecryptFrom: Color.FromArgb(21, 128, 61),
            DecryptTo: Color.FromArgb(15, 118, 110),
            Success: Color.FromArgb(21, 128, 61),
            Warning: Color.FromArgb(154, 82, 0),
            Danger: Color.FromArgb(185, 28, 28),
            InputFill: Color.White,
            InputBorder: Color.FromArgb(196, 202, 210),
            DisabledFill: Color.FromArgb(229, 231, 235),
            DisabledText: Color.FromArgb(110, 116, 124),
            DropTint: Color.FromArgb(238, 243, 255),
            Track: Color.FromArgb(229, 231, 235));

        public static readonly Palette Dark = new(
            IsDark: true,
            Background: Color.FromArgb(30, 31, 34),
            Surface: Color.FromArgb(39, 41, 45),
            SurfaceHover: Color.FromArgb(48, 51, 56),
            Border: Color.FromArgb(62, 65, 71),
            BorderStrong: Color.FromArgb(110, 116, 125),
            TextPrimary: Color.FromArgb(236, 238, 241),
            TextSecondary: Color.FromArgb(168, 174, 183),
            TextOnAccent: Color.White,
            Link: Color.FromArgb(125, 170, 255),
            Focus: Color.FromArgb(125, 170, 255),
            EncryptFrom: Color.FromArgb(59, 91, 219),
            EncryptTo: Color.FromArgb(124, 77, 230),
            DecryptFrom: Color.FromArgb(21, 128, 61),
            DecryptTo: Color.FromArgb(15, 118, 110),
            Success: Color.FromArgb(74, 222, 128),
            Warning: Color.FromArgb(251, 191, 36),
            Danger: Color.FromArgb(248, 113, 113),
            InputFill: Color.FromArgb(39, 41, 45),
            InputBorder: Color.FromArgb(80, 84, 91),
            DisabledFill: Color.FromArgb(55, 58, 63),
            DisabledText: Color.FromArgb(130, 136, 145),
            DropTint: Color.FromArgb(37, 44, 61),
            Track: Color.FromArgb(55, 58, 63));

        private static bool _listening;

        public static Palette Current { get; private set; } = Light;

        /// <summary>Phát ra khi người dùng đổi Light/Dark trong Settings của Windows.</summary>
        public static event EventHandler? Changed;

        public static void Initialize()
        {
            Current = IsSystemDark() ? Dark : Light;
            if (_listening) return;
            _listening = true;
            SystemEvents.UserPreferenceChanged += (_, e) =>
            {
                if (e.Category != UserPreferenceCategory.General) return;
                var next = IsSystemDark() ? Dark : Light;
                if (next == Current) return;
                Current = next;
                Changed?.Invoke(null, EventArgs.Empty);
            };
        }

        /// <summary>Chỉ dùng khi chụp màn hình kiểm tra / test.</summary>
        internal static void Override(Palette palette)
        {
            Current = palette;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch
            {
                return false;
            }
        }

        // ============================================
        // TYPOGRAPHY — 4 bậc: 9 / 10 / 13 / 16
        // ============================================

        public static readonly Font Small = new("Segoe UI", 9F);
        public static readonly Font Body = new("Segoe UI", 10F);
        public static readonly Font BodyStrong = new("Segoe UI Semibold", 10F);
        public static readonly Font Heading = new("Segoe UI Semibold", 13F);
        public static readonly Font Code = new("Consolas", 16F, FontStyle.Bold);

        // ============================================
        // ICON — Segoe Fluent Icons (Win11), dự phòng Segoe MDL2 Assets (Win10)
        // ============================================

        public const char GlyphFolder = '';
        public const char GlyphFile = '';
        public const char GlyphLock = '';
        public const char GlyphUpload = '';
        public const char GlyphView = '';
        public const char GlyphHide = '';
        public const char GlyphSettings = '';
        public const char GlyphMultiple = '';

        public static readonly string IconFontName = ResolveIconFont();

        public static Font IconFont(float size) => new(IconFontName, size);

        private static string ResolveIconFont()
        {
            using var fonts = new InstalledFontCollection();
            return fonts.Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";
        }

        private static readonly Dictionary<(char, int, int), Bitmap> GlyphCache = new();

        /// <summary>Vẽ một glyph icon ra bitmap (dùng cho IconRight của Guna2TextBox). Có cache.</summary>
        public static Bitmap Glyph(char glyph, Color color, int pixelSize)
        {
            var key = (glyph, color.ToArgb(), pixelSize);
            if (GlyphCache.TryGetValue(key, out var cached)) return cached;

            var bmp = new Bitmap(pixelSize, pixelSize);
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font(IconFontName, pixelSize * 0.62f, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, pixelSize, pixelSize), format);
            }
            GlyphCache[key] = bmp;
            return bmp;
        }

        // ============================================
        // Thanh tiêu đề tối cho cửa sổ có viền (dialog)
        // ============================================

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void ApplyTitleBar(IntPtr handle, Palette palette)
        {
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            int value = palette.IsDark ? 1 : 0;
            try { DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)); }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
    }
}
