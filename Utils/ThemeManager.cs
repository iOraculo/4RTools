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

        // AureumRO Light Mode — Warm Parchment & Antique Gold (aureumro.com :root tokens)
        private static readonly Color LightBackground = Color.FromArgb(253, 248, 232);   // #FDF8E8 (--bg-card)
        private static readonly Color LightCanvas = Color.FromArgb(242, 232, 204);       // #F2E8CC (--bg-deepest)
        private static readonly Color LightSidebar = Color.FromArgb(250, 244, 224);      // #FAF4E0 (--bg-base)
        private static readonly Color LightSurface = Color.FromArgb(248, 238, 209);      // #F8EED1 (--bg-card-hover)
        private static readonly Color LightInputBack = Color.FromArgb(255, 251, 237);    // #FFFBED (--bg-elevated)
        private static readonly Color LightButtonBack = Color.FromArgb(248, 238, 209);   // #F8EED1
        private static readonly Color LightButtonHover = Color.FromArgb(239, 224, 180);  // #EFE0B4
        private static readonly Color LightButtonBorder = Color.FromArgb(168, 145, 105); // #A89169 (--border-bright)
        private static readonly Color LightCardBorder = Color.FromArgb(212, 192, 152);   // #D4C098 (--border)
        private static readonly Color LightText = Color.FromArgb(42, 30, 14);            // #2A1E0E (--text-soft)
        private static readonly Color LightMutedText = Color.FromArgb(113, 90, 59);      // #715A3B (--text-muted)
        private static readonly Color LightAccent = Color.FromArgb(160, 129, 35);        // #A08123 (--gold)
        private static readonly Color LightDivider = Color.FromArgb(212, 192, 152);      // #D4C098

        // AureumRO Dark Mode — Obsidian & Royal Gold (aureumro.com homepage theme)
        private static readonly Color DarkBackground = Color.FromArgb(30, 21, 6);        // #1E1506
        private static readonly Color DarkCanvas = Color.FromArgb(11, 7, 1);             // #0B0701 (home-body)
        private static readonly Color DarkSidebar = Color.FromArgb(18, 12, 3);           // #120C03
        private static readonly Color DarkSurface = Color.FromArgb(27, 19, 4);           // #1B1304 (topnav)
        private static readonly Color DarkInputBack = Color.FromArgb(42, 29, 8);         // #2A1D08
        private static readonly Color DarkButtonBack = Color.FromArgb(46, 32, 9);        // #2E2009
        private static readonly Color DarkButtonHover = Color.FromArgb(74, 52, 14);      // #4A340E
        private static readonly Color DarkButtonBorder = Color.FromArgb(138, 104, 32);   // #8A6820
        private static readonly Color DarkCardBorder = Color.FromArgb(89, 66, 20);       // #594214
        private static readonly Color DarkText = Color.FromArgb(243, 234, 208);          // #F3EAD0
        private static readonly Color DarkMutedText = Color.FromArgb(179, 160, 121);     // #B3A079
        private static readonly Color DarkAccent = Color.FromArgb(244, 217, 122);        // #F4D97A
        private static readonly Color DarkDivider = Color.FromArgb(71, 52, 15);          // #47340F
        private static readonly Color DarkLink = Color.FromArgb(244, 217, 122);          // #F4D97A

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
                        btn.FlatAppearance.MouseOverBackColor = DarkButtonHover;
                        btn.BackColor = DarkButtonBack;
                        btn.ForeColor = DarkText;
                    }
                    else
                    {
                        btn.FlatAppearance.BorderColor = LightButtonBorder;
                        btn.FlatAppearance.MouseOverBackColor = LightButtonHover;
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
                    link.ActiveLinkColor = darkMode ? Color.FromArgb(255, 244, 207) : Color.FromArgb(108, 87, 21);
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
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = darkMode ? DarkAccent : LightText;
                    grid.EnableHeadersVisualStyles = !darkMode;
                }
                else if (control is Label)
                {
                    if (string.Equals(control.Name, "characterName", StringComparison.Ordinal))
                    {
                        control.ForeColor = darkMode ? Color.FromArgb(46, 204, 113) : Color.FromArgb(22, 163, 74);
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
