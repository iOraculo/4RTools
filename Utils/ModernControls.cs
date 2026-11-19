using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public enum SidebarIconKind
    {
        Spammer,
        Debuffs,
        BuffSkills,
        BuffItems,
        Timers,
        MacroSwitch,
        MacroSongs,
        AtkDef,
        Profiles,
        Servers
    }

    public class ModernCardPanel : Panel
    {
        private Color borderColor = Color.FromArgb(220, 226, 236);

        public Color BorderColor
        {
            get { return this.borderColor; }
            set
            {
                if (this.borderColor != value)
                {
                    this.borderColor = value;
                    Invalidate();
                }
            }
        }

        public ModernCardPanel()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(this.borderColor, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }

    public class SidebarNavButton : Control
    {
        private readonly Font regularFont;
        private readonly Font activeFont;
        private bool isActive;
        private bool isHovered;
        private bool isDarkMode;

        public string SectionKey { get; set; }
        public SidebarIconKind IconKind { get; set; }

        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    Invalidate();
                }
            }
        }

        public SidebarNavButton(string sectionKey, string label, SidebarIconKind iconKind)
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.SectionKey = sectionKey;
            this.Text = label;
            this.IconKind = iconKind;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(184, 34);
            this.regularFont = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.activeFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        public void ApplyTheme(bool darkMode)
        {
            this.isDarkMode = darkMode;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color idleBack = this.isDarkMode ? Color.FromArgb(18, 20, 26) : Color.FromArgb(236, 240, 247);
            Color hoverBack = this.isDarkMode ? Color.FromArgb(30, 34, 46) : Color.FromArgb(224, 231, 242);
            Color activeBack = this.isDarkMode ? Color.FromArgb(34, 47, 72) : Color.FromArgb(219, 234, 254);
            Color accent = this.isDarkMode ? Color.FromArgb(96, 165, 250) : Color.FromArgb(37, 99, 235);
            Color idleText = this.isDarkMode ? Color.FromArgb(203, 213, 225) : Color.FromArgb(51, 65, 85);
            Color activeText = this.isDarkMode ? Color.White : Color.FromArgb(29, 78, 216);

            Color fill = this.isActive ? activeBack : (this.isHovered ? hoverBack : idleBack);
            using (SolidBrush outerBg = new SolidBrush(idleBack))
            {
                g.FillRectangle(outerBg, 0, 0, this.Width, this.Height);
            }

            if (this.isActive || this.isHovered)
            {
                using (GraphicsPath pill = CreateRoundedRect(new Rectangle(1, 1, this.Width - 2, this.Height - 2), 6))
                using (SolidBrush bg = new SolidBrush(fill))
                {
                    g.FillPath(bg, pill);
                }
            }

            if (this.isActive)
            {
                using (GraphicsPath bar = CreateRoundedRect(new Rectangle(1, 6, 3, this.Height - 12), 2))
                using (SolidBrush accentBrush = new SolidBrush(accent))
                {
                    g.FillPath(accentBrush, bar);
                }
            }

            Color iconColor = this.isActive ? accent : (this.isDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139));
            DrawVectorIcon(g, this.IconKind, new Rectangle(13, (this.Height - 14) / 2, 14, 14), iconColor);

            Rectangle textBounds = new Rectangle(35, 0, this.Width - 41, this.Height);
            TextRenderer.DrawText(
                g,
                this.Text,
                this.isActive ? this.activeFont : this.regularFont,
                textBounds,
                this.isActive ? activeText : idleText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        internal static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            if (diameter > bounds.Width)
            {
                diameter = bounds.Width;
            }
            if (diameter > bounds.Height)
            {
                diameter = bounds.Height;
            }

            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawVectorIcon(Graphics g, SidebarIconKind kind, Rectangle r, Color color)
        {
            using (Pen pen = new Pen(color, 1.5F))
            using (SolidBrush brush = new SolidBrush(color))
            {
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;

                switch (kind)
                {
                    case SidebarIconKind.Spammer:
                        // Lightning bolt
                        Point[] bolt = new Point[]
                        {
                            new Point(cx + 1, r.Y + 1),
                            new Point(r.X + 3, cy + 1),
                            new Point(cx, cy + 1),
                            new Point(cx - 1, r.Bottom - 1),
                            new Point(r.Right - 3, cy - 1),
                            new Point(cx, cy - 1)
                        };
                        g.FillPolygon(brush, bolt);
                        break;

                    case SidebarIconKind.Debuffs:
                        // Shield
                        Point[] shield = new Point[]
                        {
                            new Point(cx, r.Y + 1),
                            new Point(r.Right - 2, r.Y + 3),
                            new Point(r.Right - 3, cy + 2),
                            new Point(cx, r.Bottom - 1),
                            new Point(r.X + 3, cy + 2),
                            new Point(r.X + 2, r.Y + 3)
                        };
                        g.DrawPolygon(pen, shield);
                        break;

                    case SidebarIconKind.BuffSkills:
                        // Plus / sparkle cross
                        g.DrawLine(pen, cx, r.Y + 1, cx, r.Bottom - 1);
                        g.DrawLine(pen, r.X + 1, cy, r.Right - 1, cy);
                        g.DrawLine(pen, r.X + 3, r.Y + 3, r.Right - 3, r.Bottom - 3);
                        g.DrawLine(pen, r.Right - 3, r.Y + 3, r.X + 3, r.Bottom - 3);
                        break;

                    case SidebarIconKind.BuffItems:
                        // Potion flask
                        g.DrawLine(pen, cx - 2, r.Y + 2, cx + 2, r.Y + 2);
                        g.DrawEllipse(pen, r.X + 2, r.Y + 5, r.Width - 4, r.Height - 6);
                        break;

                    case SidebarIconKind.Timers:
                        // Clock circle with hands
                        g.DrawEllipse(pen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                        g.DrawLine(pen, cx, cy, cx, r.Y + 4);
                        g.DrawLine(pen, cx, cy, r.Right - 4, cy);
                        break;

                    case SidebarIconKind.MacroSwitch:
                        // Swap arrows
                        g.DrawLine(pen, r.X + 2, cy - 3, r.Right - 2, cy - 3);
                        g.DrawLine(pen, r.Right - 5, cy - 6, r.Right - 2, cy - 3);
                        g.DrawLine(pen, r.X + 2, cy + 3, r.Right - 2, cy + 3);
                        g.DrawLine(pen, r.X + 5, cy + 6, r.X + 2, cy + 3);
                        break;

                    case SidebarIconKind.MacroSongs:
                        // Musical note
                        g.FillEllipse(brush, r.X + 2, r.Bottom - 6, 5, 4);
                        g.DrawLine(pen, r.X + 6, r.Bottom - 4, r.X + 6, r.Y + 2);
                        g.DrawLine(pen, r.X + 6, r.Y + 2, r.Right - 2, r.Y + 4);
                        break;

                    case SidebarIconKind.AtkDef:
                        // Crossed blades
                        g.DrawLine(pen, r.X + 2, r.Y + 2, r.Right - 2, r.Bottom - 2);
                        g.DrawLine(pen, r.Right - 2, r.Y + 2, r.X + 2, r.Bottom - 2);
                        break;

                    case SidebarIconKind.Profiles:
                        // User silhouette
                        g.DrawEllipse(pen, cx - 3, r.Y + 1, 6, 6);
                        g.DrawArc(pen, r.X + 2, cy + 1, r.Width - 4, 8, 180, 180);
                        break;

                    case SidebarIconKind.Servers:
                        // Stacked server bars
                        g.DrawRectangle(pen, r.X + 2, r.Y + 2, r.Width - 4, 4);
                        g.DrawRectangle(pen, r.X + 2, r.Y + 8, r.Width - 4, 4);
                        break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.regularFont != null)
                {
                    this.regularFont.Dispose();
                }
                if (this.activeFont != null)
                {
                    this.activeFont.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    public class SegmentedTabButton : Control
    {
        private readonly Font regularFont;
        private readonly Font activeFont;
        private bool isActive;
        private bool isHovered;
        private bool isDarkMode;

        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    Invalidate();
                }
            }
        }

        public SegmentedTabButton(string text)
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.Text = text;
            this.Cursor = Cursors.Hand;
            this.regularFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            this.activeFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        }

        public void ApplyTheme(bool darkMode)
        {
            this.isDarkMode = darkMode;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color cardBack = this.isDarkMode ? Color.FromArgb(30, 32, 38) : Color.White;
            Color activeFill = this.isDarkMode ? Color.FromArgb(59, 130, 246) : Color.FromArgb(37, 99, 235);
            Color idleFill = this.isDarkMode ? Color.FromArgb(40, 44, 56) : Color.FromArgb(241, 245, 249);
            Color hoverFill = this.isDarkMode ? Color.FromArgb(50, 56, 70) : Color.FromArgb(226, 232, 240);
            Color border = this.isActive
                ? activeFill
                : (this.isDarkMode ? Color.FromArgb(62, 68, 84) : Color.FromArgb(203, 213, 225));
            Color textColor = this.isActive
                ? Color.White
                : (this.isDarkMode ? Color.FromArgb(203, 213, 225) : Color.FromArgb(51, 65, 85));

            using (SolidBrush outer = new SolidBrush(cardBack))
            {
                g.FillRectangle(outer, 0, 0, this.Width, this.Height);
            }

            Color fill = this.isActive ? activeFill : (this.isHovered ? hoverFill : idleFill);
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using (GraphicsPath pill = SidebarNavButton.CreateRoundedRect(rect, 5))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, 1))
            {
                g.FillPath(brush, pill);
                g.DrawPath(pen, pill);
            }

            TextRenderer.DrawText(
                g,
                this.Text,
                this.isActive ? this.activeFont : this.regularFont,
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.regularFont != null)
                {
                    this.regularFont.Dispose();
                }
                if (this.activeFont != null)
                {
                    this.activeFont.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    public class StatusDot : Control
    {
        private bool isConnected;
        private Color? customColor;

        public bool IsConnected
        {
            get { return this.isConnected; }
            set
            {
                if (this.isConnected != value)
                {
                    this.isConnected = value;
                    Invalidate();
                }
            }
        }

        public Color? CustomColor
        {
            get { return this.customColor; }
            set
            {
                if (this.customColor != value)
                {
                    this.customColor = value;
                    Invalidate();
                }
            }
        }

        public StatusDot()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.DoubleBuffered = true;
            this.Size = new Size(10, 10);
            this.Tag = ThemeManager.PRESERVE_COLORS_TAG;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color dotColor = this.customColor.HasValue
                ? this.customColor.Value
                : (this.isConnected ? Color.FromArgb(34, 197, 94) : Color.FromArgb(148, 163, 184));
            using (SolidBrush brush = new SolidBrush(dotColor))
            {
                e.Graphics.FillEllipse(brush, 1, 1, this.Width - 3, this.Height - 3);
            }
        }
    }
}
