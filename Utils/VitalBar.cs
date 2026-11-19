using System;
using System.Drawing;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public class VitalBar : Control
    {
        private uint currentValue;
        private uint maxValue;
        private readonly string labelPrefix;
        private readonly Color fillColor;
        private readonly Font barFont;
        private Color trackColor;
        private Color borderColor;
        private Color textColor;

        public VitalBar(string prefix, Color barColor)
        {
            this.DoubleBuffered = true;
            this.labelPrefix = prefix ?? string.Empty;
            this.fillColor = barColor;
            this.barFont = new Font("Microsoft Sans Serif", 7.5F, FontStyle.Bold);
            this.Font = this.barFont;
            ApplyTheme(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.barFont != null)
            {
                this.barFont.Dispose();
            }

            base.Dispose(disposing);
        }

        public void ApplyTheme(bool darkMode)
        {
            this.trackColor = darkMode ? Color.FromArgb(48, 52, 64) : Color.FromArgb(230, 232, 236);
            this.borderColor = darkMode ? Color.FromArgb(80, 86, 102) : Color.FromArgb(180, 184, 192);
            this.textColor = darkMode ? Color.White : Color.FromArgb(30, 30, 35);
            Invalidate();
        }

        public void UpdateValues(uint current, uint max)
        {
            if (this.currentValue == current && this.maxValue == max)
            {
                return;
            }

            this.currentValue = current;
            this.maxValue = max;
            Invalidate();
        }

        public void ClearValues()
        {
            UpdateValues(0, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            using (SolidBrush trackBrush = new SolidBrush(this.trackColor))
            {
                e.Graphics.FillRectangle(trackBrush, bounds);
            }

            string text;
            if (this.maxValue > 0)
            {
                double ratio = Math.Min(1.0, Math.Max(0.0, (double)this.currentValue / (double)this.maxValue));
                int fillWidth = (int)Math.Round(bounds.Width * ratio);
                if (fillWidth > 0)
                {
                    using (SolidBrush fillBrush = new SolidBrush(this.fillColor))
                    {
                        e.Graphics.FillRectangle(fillBrush, 1, 1, Math.Max(0, fillWidth - 1), Math.Max(0, bounds.Height - 1));
                    }
                }

                int percent = (int)Math.Round(ratio * 100.0);
                text = string.Format("{0}: {1:N0} / {2:N0} ({3}%)", this.labelPrefix, this.currentValue, this.maxValue, percent);
            }
            else
            {
                text = this.labelPrefix + ": -- / --";
            }

            using (Pen borderPen = new Pen(this.borderColor))
            {
                e.Graphics.DrawRectangle(borderPen, bounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                text,
                this.Font,
                bounds,
                this.textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }
}
