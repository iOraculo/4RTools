using System;
using System.Drawing;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public static class ThemeManager
    {
        public const string PRESERVE_COLORS_TAG = "preserveColors";
        public const string TAG_CANVAS = "themeCanvas";
        public const string TAG_SIDEBAR = "themeSidebar";
        public const string TAG_TOPBAR = "themeTopbar";
        public const string TAG_FOOTER = "themeFooter";
        public const string TAG_MUTED_TEXT = "themeMuted";
        public const string TAG_ACCENT_TEXT = "themeAccent";

        private static readonly Color LightBackground = Color.White;
        private static readonly Color LightCanvas = Color.FromArgb(243, 245, 249);
        private static readonly Color LightSidebar = Color.FromArgb(236, 240, 247);
        private static readonly Color LightSurface = Color.White;
        private static readonly Color LightInputBack = Color.White;
        private static readonly Color LightButtonBack = Color.FromArgb(248, 250, 252);
        private static readonly Color LightButtonBorder = Color.FromArgb(203, 213, 225);
        private static readonly Color LightCardBorder = Color.FromArgb(220, 226, 236);
        private static readonly Color LightText = Color.FromArgb(30, 36, 48);
        private static readonly Color LightMutedText = Color.FromArgb(100, 116, 139);
        private static readonly Color LightAccent = Color.FromArgb(37, 99, 235);
        private static readonly Color LightDivider = Color.FromArgb(220, 226, 236);

        private static readonly Color DarkBackground = Color.FromArgb(30, 32, 38);
        private static readonly Color DarkCanvas = Color.FromArgb(22, 24, 30);
        private static readonly Color DarkSidebar = Color.FromArgb(18, 20, 26);
        private static readonly Color DarkSurface = Color.FromArgb(26, 29, 36);
        private static readonly Color DarkInputBack = Color.FromArgb(52, 56, 68);
        private static readonly Color DarkButtonBack = Color.FromArgb(48, 53, 66);
        private static readonly Color DarkButtonBorder = Color.FromArgb(72, 78, 94);
        private static readonly Color DarkCardBorder = Color.FromArgb(48, 53, 66);
        private static readonly Color DarkText = Color.FromArgb(236, 239, 244);
        private static readonly Color DarkMutedText = Color.FromArgb(148, 163, 184);
        private static readonly Color DarkAccent = Color.FromArgb(96, 165, 250);
        private static readonly Color DarkDivider = Color.FromArgb(52, 58, 72);
        private static readonly Color DarkLink = Color.FromArgb(116, 179, 255);

        public static bool IsDarkMode { get; private set; }

        public static Color GetDefaultTextColor()
        {
            return IsDarkMode ? DarkText : LightText;
        }

        public static void ApplyCurrentTheme(Control root)
        {
            ApplyTheme(root, IsDarkMode);
        }

        public static void ApplyTheme(Control root, bool darkMode)
        {
            IsDarkMode = darkMode;
            if (root == null || root.IsDisposed)
            {
                return;
            }

            root.SuspendLayout();
            try
            {
                ApplyControlTheme(root, darkMode);
            }
            finally
            {
                root.ResumeLayout(true);
            }
        }

        private static void ApplyControlTheme(Control control, bool darkMode)
        {
            if (control == null || control.IsDisposed)
            {
                return;
            }

            string tag = control.Tag as string;
            bool preserve = string.Equals(tag, PRESERVE_COLORS_TAG, StringComparison.Ordinal);

            if (!preserve)
            {
                if (control is VitalBar)
                {
                    ((VitalBar)control).ApplyTheme(darkMode);
                }
                else if (control is SidebarNavButton)
                {
                    ((SidebarNavButton)control).ApplyTheme(darkMode);
                }
                else if (control is SegmentedTabButton)
                {
                    ((SegmentedTabButton)control).ApplyTheme(darkMode);
                }
                else if (control is ModernCardPanel)
                {
                    ModernCardPanel card = (ModernCardPanel)control;
                    card.BackColor = darkMode ? DarkBackground : LightBackground;
                    card.ForeColor = darkMode ? DarkText : LightText;
                    card.BorderColor = darkMode ? DarkCardBorder : LightCardBorder;
                }
                else if (control is Form)
                {
                    if (string.Equals(tag, TAG_CANVAS, StringComparison.Ordinal))
                    {
                        control.BackColor = darkMode ? DarkCanvas : LightCanvas;
                    }
                    else
                    {
                        control.BackColor = darkMode ? DarkBackground : LightBackground;
                    }
                    control.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is MdiClient)
                {
                    control.BackColor = darkMode ? DarkCanvas : LightCanvas;
                }
                else if (control is TabPage)
                {
                    TabPage tabPage = (TabPage)control;
                    tabPage.UseVisualStyleBackColor = !darkMode;
                    tabPage.BackColor = darkMode ? DarkBackground : LightBackground;
                    tabPage.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is GroupBox)
                {
                    control.BackColor = darkMode ? DarkBackground : LightBackground;
                    control.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is Panel)
                {
                    if (control.Height <= 2 || control.Width <= 2)
                    {
                        control.BackColor = darkMode ? DarkDivider : LightDivider;
                    }
                    else if (string.Equals(tag, TAG_CANVAS, StringComparison.Ordinal))
                    {
                        control.BackColor = darkMode ? DarkCanvas : LightCanvas;
                        control.ForeColor = darkMode ? DarkText : LightText;
                    }
                    else if (string.Equals(tag, TAG_SIDEBAR, StringComparison.Ordinal))
                    {
                        control.BackColor = darkMode ? DarkSidebar : LightSidebar;
                        control.ForeColor = darkMode ? DarkText : LightText;
                    }
                    else if (string.Equals(tag, TAG_TOPBAR, StringComparison.Ordinal)
                        || string.Equals(tag, TAG_FOOTER, StringComparison.Ordinal)
                        || string.Equals(control.Name, "panelFooter", StringComparison.Ordinal))
                    {
                        control.BackColor = darkMode ? DarkSurface : LightSurface;
                        control.ForeColor = darkMode ? DarkText : LightText;
                    }
                    else
                    {
                        control.BackColor = darkMode ? DarkBackground : LightBackground;
                        control.ForeColor = darkMode ? DarkText : LightText;
                    }
                }
                else if (control is Button)
                {
                    Button btn = (Button)control;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    if (darkMode)
                    {
                        btn.FlatAppearance.BorderColor = DarkButtonBorder;
                        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 68, 84);
                        btn.BackColor = DarkButtonBack;
                        btn.ForeColor = DarkText;
                    }
                    else
                    {
                        btn.FlatAppearance.BorderColor = LightButtonBorder;
                        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(236, 242, 250);
                        btn.BackColor = LightButtonBack;
                        btn.ForeColor = LightText;
                    }
                }
                else if (control is TextBox || control is NumericUpDown || control is ComboBox || control is ListBox)
                {
                    control.BackColor = darkMode ? DarkInputBack : LightInputBack;
                    control.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is LinkLabel)
                {
                    LinkLabel link = (LinkLabel)control;
                    link.LinkColor = darkMode ? DarkLink : LightAccent;
                    link.VisitedLinkColor = darkMode ? DarkLink : LightAccent;
                    link.ActiveLinkColor = darkMode ? Color.White : Color.FromArgb(29, 78, 216);
                    link.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is DataGridView)
                {
                    DataGridView grid = (DataGridView)control;
                    grid.BackgroundColor = darkMode ? DarkSurface : LightCanvas;
                    grid.GridColor = darkMode ? DarkDivider : LightDivider;
                    grid.DefaultCellStyle.BackColor = darkMode ? DarkInputBack : LightInputBack;
                    grid.DefaultCellStyle.ForeColor = darkMode ? DarkText : LightText;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = darkMode ? DarkSurface : LightSidebar;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = darkMode ? DarkText : LightText;
                    grid.EnableHeadersVisualStyles = !darkMode;
                }
                else if (control is Label)
                {
                    if (string.Equals(control.Name, "characterName", StringComparison.Ordinal))
                    {
                        control.ForeColor = darkMode ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
                    }
                    else if (string.Equals(tag, TAG_MUTED_TEXT, StringComparison.Ordinal))
                    {
                        control.ForeColor = darkMode ? DarkMutedText : LightMutedText;
                    }
                    else if (string.Equals(tag, TAG_ACCENT_TEXT, StringComparison.Ordinal))
                    {
                        control.ForeColor = darkMode ? DarkAccent : LightAccent;
                    }
                    else
                    {
                        control.ForeColor = darkMode ? DarkText : LightText;
                    }
                }
                else if (control is CheckBox || control is RadioButton)
                {
                    control.ForeColor = darkMode ? DarkText : LightText;
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyControlTheme(child, darkMode);
            }
        }
    }
}
