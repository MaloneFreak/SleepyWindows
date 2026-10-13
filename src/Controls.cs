using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SleepyWindows
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(19, 22, 43);
        public static readonly Color Surface = Color.FromArgb(32, 36, 63);
        public static readonly Color SurfaceHover = Color.FromArgb(42, 47, 82);
        public static readonly Color SurfacePressed = Color.FromArgb(52, 58, 99);
        public static readonly Color Accent = Color.FromArgb(140, 158, 255);
        public static readonly Color AccentHover = Color.FromArgb(162, 176, 255);
        public static readonly Color AccentPressed = Color.FromArgb(122, 140, 240);
        public static readonly Color OnAccent = Color.FromArgb(14, 17, 36);
        public static readonly Color Text = Color.FromArgb(230, 233, 248);
        public static readonly Color Muted = Color.FromArgb(149, 155, 192);
        public static readonly Color Disabled = Color.FromArgb(90, 95, 130);
        public static readonly Color Danger = Color.FromArgb(255, 154, 154);

        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal enum ButtonKind
    {
        Normal,
        Accent,
        Danger,
        /// <summary>Quiet button with muted text; used for the language button.</summary>
        Ghost,
    }

    /// <summary>
    /// A rounded, flat button for the dark theme. Keeps all the normal Button behavior
    /// (click, Enter/Space, Tab focus); only the drawing is custom.
    /// Long labels (other languages) are shrunk a little, then wrapped onto two lines, so they still fit.
    /// </summary>
    internal sealed class FlatButton : Button
    {
        private const float MinSingleLineSize = 8.5f;
        private const float MinWrappedSize = 7.5f;

        private bool hover;
        private bool pressed;
        private bool selected;
        private bool dimmed;
        private bool showGlobe;

        private Font fittedFont;
        private TextFormatFlags fittedFlags;
        private string fittedKey;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
        }

        public ButtonKind Kind { get; set; } = ButtonKind.Normal;

        /// <summary>Highlighted as the chosen option of a group (action or unit).</summary>
        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        /// <summary>Drawn greyed out but still clickable (so it can explain why it's unavailable).</summary>
        public bool Dimmed
        {
            get { return dimmed; }
            set { dimmed = value; Invalidate(); }
        }

        /// <summary>Draws a small globe before the text (language button).</summary>
        public bool ShowGlobe
        {
            get { return showGlobe; }
            set { showGlobe = value; Invalidate(); }
        }

        protected override void OnTextChanged(EventArgs e) { fittedKey = null; Invalidate(); base.OnTextChanged(e); }
        protected override void OnFontChanged(EventArgs e) { fittedKey = null; Invalidate(); base.OnFontChanged(e); }
        protected override void OnSizeChanged(EventArgs e) { fittedKey = null; Invalidate(); base.OnSizeChanged(e); }

        protected override void Dispose(bool disposing)
        {
            if (disposing && fittedFont != null)
            {
                fittedFont.Dispose();
                fittedFont = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Everything is drawn in OnPaint.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Background);

            Color fill;
            Color text;
            bool accentLook = Kind == ButtonKind.Accent || selected;

            if (Kind == ButtonKind.Ghost)
            {
                fill = pressed ? Theme.SurfacePressed : hover ? Theme.SurfaceHover : Theme.Surface;
                text = hover ? Theme.Text : Theme.Muted;
            }
            else if (accentLook && Enabled)
            {
                fill = pressed ? Theme.AccentPressed : hover ? Theme.AccentHover : Theme.Accent;
                text = Theme.OnAccent;
            }
            else
            {
                fill = pressed && Enabled ? Theme.SurfacePressed : hover && Enabled ? Theme.SurfaceHover : Theme.Surface;
                text = !Enabled || dimmed ? Theme.Disabled
                     : Kind == ButtonKind.Danger ? Theme.Danger
                     : Theme.Text;
            }

            float scale = g.DpiX / 96f;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            using (GraphicsPath path = Theme.RoundedRect(bounds, 8 * scale))
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }

            if (Focused && ShowFocusCues)
            {
                float inset = 1.5f * scale;
                var ring = RectangleF.Inflate(bounds, -inset, -inset);
                using (GraphicsPath path = Theme.RoundedRect(ring, 7 * scale))
                using (var pen = new Pen(accentLook && Kind != ButtonKind.Ghost ? Theme.OnAccent : Theme.Accent, 1.5f * scale))
                {
                    g.DrawPath(pen, path);
                }
            }

            if (showGlobe)
            {
                DrawGlobeAndText(g, text, fill, scale);
                return;
            }

            EnsureFittedFont(g, scale);
            var textBounds = Rectangle.Inflate(ClientRectangle, -(int)(5 * scale), -(int)(2 * scale));
            TextRenderer.DrawText(g, Text, fittedFont, textBounds, text, fill, fittedFlags);
        }

        /// <summary>
        /// Picks the biggest font (down to a minimum) at which the label fits on one line;
        /// otherwise wraps it onto two lines; as a last resort, cuts it with "...".
        /// </summary>
        private void EnsureFittedFont(Graphics g, float scale)
        {
            string key = Text + "|" + Width + "x" + Height + "|" + Font.Name + "|" + Font.SizeInPoints;
            if (key == fittedKey && fittedFont != null)
            {
                return;
            }

            if (fittedFont != null)
            {
                fittedFont.Dispose();
                fittedFont = null;
            }
            fittedKey = key;

            const TextFormatFlags centered = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                                             | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            int availableWidth = Math.Max(1, Width - (int)(12 * scale));
            int availableHeight = Math.Max(1, Height - (int)(6 * scale));
            var unlimited = new Size(int.MaxValue, int.MaxValue);
            float baseSize = Font.SizeInPoints;

            for (float size = baseSize; size >= MinSingleLineSize - 0.01f; size -= 0.5f)
            {
                var font = new Font(Font.FontFamily, size, Font.Style);
                Size measured = TextRenderer.MeasureText(g, Text, font, unlimited,
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                if (measured.Width <= availableWidth)
                {
                    fittedFont = font;
                    fittedFlags = centered | TextFormatFlags.SingleLine;
                    return;
                }
                font.Dispose();
            }

            for (float size = baseSize - 0.5f; size >= MinWrappedSize - 0.01f; size -= 0.5f)
            {
                var font = new Font(Font.FontFamily, size, Font.Style);
                Size measured = TextRenderer.MeasureText(g, Text, font, new Size(availableWidth, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
                if (measured.Width <= availableWidth && measured.Height <= availableHeight)
                {
                    fittedFont = font;
                    fittedFlags = centered | TextFormatFlags.WordBreak;
                    return;
                }
                font.Dispose();
            }

            fittedFont = new Font(Font.FontFamily, MinWrappedSize, Font.Style);
            fittedFlags = centered | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        }

        private void DrawGlobeAndText(Graphics g, Color color, Color fill, float scale)
        {
            const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                                          | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter;
            Size textSize = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue), flags);
            float globe = 14 * scale;
            float gap = 6 * scale;
            float x = (Width - (globe + gap + textSize.Width)) / 2f;
            var circle = new RectangleF(x, (Height - globe) / 2f, globe, globe);

            using (var pen = new Pen(color, 1.2f * scale))
            {
                g.DrawEllipse(pen, circle);
                float meridian = globe * 0.42f;
                g.DrawEllipse(pen, circle.X + (globe - meridian) / 2f, circle.Y, meridian, globe);
                g.DrawLine(pen, circle.X, circle.Y + globe / 2f, circle.Right, circle.Y + globe / 2f);
            }

            var textBounds = new Rectangle((int)(x + globe + gap), 0, textSize.Width + 2, Height);
            TextRenderer.DrawText(g, Text, Font, textBounds, color, fill, flags);
        }
    }

    /// <summary>Colors for the dark language menu.</summary>
    internal sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Surface;
        public override Color ImageMarginGradientBegin => Theme.Surface;
        public override Color ImageMarginGradientMiddle => Theme.Surface;
        public override Color ImageMarginGradientEnd => Theme.Surface;
        public override Color MenuBorder => Theme.SurfacePressed;
        public override Color MenuItemBorder => Theme.SurfaceHover;
        public override Color MenuItemSelected => Theme.SurfaceHover;
        public override Color MenuItemSelectedGradientBegin => Theme.SurfaceHover;
        public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;
        public override Color MenuItemPressedGradientBegin => Theme.SurfacePressed;
        public override Color MenuItemPressedGradientEnd => Theme.SurfacePressed;
        public override Color CheckBackground => Theme.Surface;
        public override Color CheckSelectedBackground => Theme.SurfaceHover;
        public override Color CheckPressedBackground => Theme.SurfacePressed;
        public override Color SeparatorDark => Theme.SurfacePressed;
        public override Color SeparatorLight => Theme.Surface;
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Disabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(Theme.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // An accent dot instead of the default (black) check mark, which is invisible on dark.
            Rectangle r = e.ImageRectangle;
            float d = Math.Min(r.Width, r.Height) * 0.45f;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(Theme.Accent))
            {
                e.Graphics.FillEllipse(brush, r.X + (r.Width - d) / 2f, r.Y + (r.Height - d) / 2f, d, d);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Rectangle r = e.Item.ContentRectangle;
            int y = r.Top + r.Height / 2;
            using (var pen = new Pen(Theme.SurfacePressed))
            {
                e.Graphics.DrawLine(pen, r.Left + 4, y, r.Right - 4, y);
            }
        }
    }

    /// <summary>A rounded box holding a borderless, digits-only TextBox.</summary>
    internal sealed class NumberBox : Panel
    {
        public NumberBox()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);

            TextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 5,
                ShortcutsEnabled = true,
            };
            TextBox.KeyPress += OnTextBoxKeyPress;
            TextBox.TextChanged += OnTextBoxTextChanged;
            TextBox.GotFocus += (s, e) => Invalidate();
            TextBox.LostFocus += (s, e) => Invalidate();
            Controls.Add(TextBox);

            Cursor = Cursors.IBeam;
            Click += (s, e) => TextBox.Focus();
        }

        public TextBox TextBox { get; }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int pad = Math.Max(4, Height / 6);
            TextBox.SetBounds(pad, (Height - TextBox.Height) / 2, Width - 2 * pad, TextBox.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float scale = g.DpiX / 96f;
            var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (GraphicsPath path = Theme.RoundedRect(bounds, 8 * scale))
            {
                using (var brush = new SolidBrush(Theme.Surface))
                {
                    g.FillPath(brush, path);
                }
                if (TextBox.Focused)
                {
                    using (var pen = new Pen(Theme.Accent, 1.5f * scale))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }

        private static void OnTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void OnTextBoxTextChanged(object sender, EventArgs e)
        {
            // Strip anything that isn't a digit (e.g. pasted text).
            string text = TextBox.Text;
            var digits = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c >= '0' && c <= '9')
                {
                    digits.Append(c);
                }
            }

            if (digits.Length != text.Length)
            {
                int caret = Math.Max(0, TextBox.SelectionStart - (text.Length - digits.Length));
                TextBox.Text = digits.ToString();
                TextBox.SelectionStart = Math.Min(caret, TextBox.Text.Length);
            }
        }
    }
}
