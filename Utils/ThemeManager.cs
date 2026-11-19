using System;
using System.Drawing;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public static class ThemeManager
    {
        public const string PRESERVE_COLORS_TAG = "preserveColors";

        private static readonly Color LightBackground = Color.White;
        private static readonly Color LightSurface = Color.WhiteSmoke;
        private static readonly Color LightInputBack = Color.White;
        private static readonly Color LightText = SystemColors.ControlText;
        private static readonly Color LightDivider = Color.Silver;

        private static readonly Color DarkBackground = Color.FromArgb(30, 32, 38);
        private static readonly Color DarkSurface = Color.FromArgb(40, 43, 52);
        private static readonly Color DarkInputBack = Color.FromArgb(52, 56, 68);
        private static readonly Color DarkButtonBack = Color.FromArgb(58, 63, 76);
        private static readonly Color DarkText = Color.FromArgb(236, 239, 244);
        private static readonly Color DarkDivider = Color.FromArgb(72, 76, 88);
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

            bool preserve = string.Equals(control.Tag as string, PRESERVE_COLORS_TAG, StringComparison.Ordinal);

            if (!preserve)
            {
                if (control is VitalBar)
                {
                    ((VitalBar)control).ApplyTheme(darkMode);
                }
                else if (control is Form)
                {
                    control.BackColor = darkMode ? DarkBackground : LightBackground;
                    control.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is MdiClient)
                {
                    control.BackColor = darkMode ? DarkBackground : LightBackground;
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
                    else if (string.Equals(control.Name, "panelFooter", StringComparison.Ordinal))
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
                    if (darkMode)
                    {
                        btn.FlatStyle = FlatStyle.Flat;
                        btn.FlatAppearance.BorderColor = DarkDivider;
                        btn.BackColor = DarkButtonBack;
                        btn.ForeColor = DarkText;
                    }
                    else
                    {
                        btn.FlatStyle = FlatStyle.Standard;
                        btn.UseVisualStyleBackColor = true;
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
                    link.LinkColor = darkMode ? DarkLink : Color.Blue;
                    link.VisitedLinkColor = darkMode ? DarkLink : Color.Purple;
                    link.ForeColor = darkMode ? DarkText : LightText;
                }
                else if (control is DataGridView)
                {
                    DataGridView grid = (DataGridView)control;
                    grid.BackgroundColor = darkMode ? DarkSurface : SystemColors.AppWorkspace;
                    grid.DefaultCellStyle.BackColor = darkMode ? DarkInputBack : LightInputBack;
                    grid.DefaultCellStyle.ForeColor = darkMode ? DarkText : LightText;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = darkMode ? DarkSurface : SystemColors.Control;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = darkMode ? DarkText : LightText;
                    grid.EnableHeadersVisualStyles = !darkMode;
                }
                else if (control is Label)
                {
                    if (string.Equals(control.Name, "characterName", StringComparison.Ordinal))
                    {
                        control.ForeColor = darkMode ? Color.LightGreen : Color.DarkGreen;
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
