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
    }

    /// <summary>
    /// A rounded, flat button for the dark theme. Keeps all the normal Button behavior
    /// (click, Enter/Space, Tab focus); only the drawing is custom.
    /// </summary>
    internal sealed class FlatButton : Button
    {
        private bool hover;
        private bool pressed;
        private bool selected;
        private bool dimmed;

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

            if (accentLook && Enabled)
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
                using (var pen = new Pen(accentLook ? Theme.OnAccent : Theme.Accent, 1.5f * scale))
                {
                    g.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text, fill,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
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
