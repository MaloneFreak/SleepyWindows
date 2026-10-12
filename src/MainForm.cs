using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SleepyWindows
{
    internal sealed class MainForm : Form
    {
        private const int MaxSeconds = 999 * 3600;
        private const int SnoozeSeconds = 15 * 60;
        private const int WarningSeconds = 60;

        private static readonly string[] PresetLabels = { "10 min", "20 min", "30 min", "45 min", "1 h", "1h30", "2 h", "3 h" };
        private static readonly int[] PresetMinutes = { 10, 20, 30, 45, 60, 90, 120, 180 };
        private static readonly int[] UnitSeconds = { 1, 60, 3600 };

        private readonly Panel setupPanel;
        private readonly Panel countdownPanel;
        private readonly FlatButton[] actionButtons = new FlatButton[4];
        private readonly FlatButton[] unitButtons = new FlatButton[3];
        private readonly NumberBox customBox;
        private readonly FlatButton scheduleButton;
        private readonly FlatButton snoozeButton;
        private readonly FlatButton cancelButton;
        private readonly Label countdownTitle;
        private readonly Label countdownLabel;
        private readonly Label targetLabel;
        private readonly Label hintLabel;
        private readonly Timer ticker;
        private readonly bool hibernateAllowed;

        private PowerAction selectedAction = PowerAction.Shutdown;
        private int selectedUnit = 1; // minutes

        private bool running;
        private bool warned;
        private PowerAction activeAction;
        private DateTime targetUtc;

        public MainForm()
        {
            hibernateAllowed = NativeMethods.HibernateAllowed();

            SuspendLayout();

            // Layout is written for 96 DPI and scaled automatically on high-DPI screens.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(400, 382);
            Font = new Font("Segoe UI", 10F);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = Strings.AppName;
            Icon = LoadWindowIcon() ?? Icon;

            // Header
            var logo = new PictureBox
            {
                Bounds = new Rectangle(20, 17, 30, 30),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = LoadLogo(),
            };
            Controls.Add(logo);
            Controls.Add(new Label
            {
                AutoSize = true,
                Location = new Point(57, 18),
                Text = Strings.AppName,
                Font = new Font("Segoe UI Semibold", 15F),
                ForeColor = Theme.Text,
            });

            // ---- Setup screen ----
            setupPanel = new Panel { Bounds = new Rectangle(0, 58, 400, 324) };
            Controls.Add(setupPanel);

            AddSectionLabel(setupPanel, Strings.WhatToDo, 8);
            for (int i = 0; i < actionButtons.Length; i++)
            {
                var action = (PowerAction)i;
                var button = AddButton(setupPanel, Strings.ActionName(action), 20 + i * 91, 30, 87, 38);
                button.Click += (s, e) => SelectAction(action);
                actionButtons[i] = button;
            }
            actionButtons[(int)PowerAction.Hibernate].Dimmed = !hibernateAllowed;

            AddSectionLabel(setupPanel, Strings.InHowLong, 84);
            for (int i = 0; i < PresetLabels.Length; i++)
            {
                int seconds = PresetMinutes[i] * 60;
                var button = AddButton(setupPanel, PresetLabels[i], 20 + (i % 4) * 91, 106 + (i / 4) * 44, 87, 40);
                button.Click += (s, e) => Start(seconds);
            }

            AddSectionLabel(setupPanel, Strings.CustomTime, 206);
            customBox = new NumberBox { Bounds = new Rectangle(20, 228, 84, 38) };
            customBox.TextBox.Font = new Font("Segoe UI", 12F);
            customBox.TextBox.Text = "90";
            setupPanel.Controls.Add(customBox);

            string[] unitNames = { Strings.Seconds, Strings.Minutes, Strings.Hours };
            for (int i = 0; i < unitButtons.Length; i++)
            {
                int unit = i;
                var button = AddButton(setupPanel, unitNames[i], 110 + i * 50, 228, 46, 38);
                button.Click += (s, e) => SelectUnit(unit);
                unitButtons[i] = button;
            }

            scheduleButton = AddButton(setupPanel, Strings.Schedule, 264, 228, 116, 38);
            scheduleButton.Kind = ButtonKind.Accent;
            scheduleButton.Click += (s, e) => StartCustom();

            var cancelExisting = new LinkLabel
            {
                AutoSize = true,
                Location = new Point(18, 282),
                Text = Strings.CancelExisting,
                Font = new Font("Segoe UI", 9F),
                LinkColor = Theme.Muted,
                ActiveLinkColor = Theme.Accent,
                VisitedLinkColor = Theme.Muted,
                LinkBehavior = LinkBehavior.HoverUnderline,
            };
            cancelExisting.LinkClicked += (s, e) => CancelExistingSchedule();
            setupPanel.Controls.Add(cancelExisting);

            // ---- Countdown screen ----
            countdownPanel = new Panel { Bounds = new Rectangle(0, 58, 400, 324), Visible = false };
            Controls.Add(countdownPanel);

            countdownTitle = AddCenteredLabel(countdownPanel, 14, 24, 10F, Theme.Muted);
            countdownLabel = AddCenteredLabel(countdownPanel, 38, 72, 36F, Theme.Text);
            countdownLabel.Font = new Font("Segoe UI Semilight", 36F);
            targetLabel = AddCenteredLabel(countdownPanel, 112, 24, 10F, Theme.Muted);

            snoozeButton = AddButton(countdownPanel, Strings.Snooze, 20, 150, 176, 42);
            snoozeButton.Click += (s, e) => Snooze();
            cancelButton = AddButton(countdownPanel, Strings.Cancel, 204, 150, 176, 42);
            cancelButton.Kind = ButtonKind.Danger;
            cancelButton.Click += (s, e) => CancelTimer();

            hintLabel = AddCenteredLabel(countdownPanel, 210, 72, 9F, Theme.Muted);
            hintLabel.TextAlign = ContentAlignment.TopCenter;

            ticker = new Timer { Interval = 200 };
            ticker.Tick += (s, e) => Tick();

            AcceptButton = scheduleButton;
            SelectAction(PowerAction.Shutdown);
            SelectUnit(1);

            ResumeLayout(false);
            PerformLayout();
        }

        // ---------------- UI helpers ----------------

        private static Icon LoadWindowIcon()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SleepyWindows.window.ico"))
                {
                    return stream == null ? null : new Icon(stream);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Image LoadLogo()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SleepyWindows.logo.png"))
                {
                    // Copy into a Bitmap so the stream can be closed safely.
                    return stream == null ? null : new Bitmap(Image.FromStream(stream));
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void AddSectionLabel(Control parent, string text, int y)
        {
            parent.Controls.Add(new Label
            {
                AutoSize = true,
                Location = new Point(18, y),
                Text = text,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.Muted,
            });
        }

        private static Label AddCenteredLabel(Control parent, int y, int height, float fontSize, Color color)
        {
            var label = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(20, y, 360, height),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", fontSize),
                ForeColor = color,
            };
            parent.Controls.Add(label);
            return label;
        }

        private static FlatButton AddButton(Control parent, string text, int x, int y, int width, int height)
        {
            var button = new FlatButton { Text = text, Bounds = new Rectangle(x, y, width, height) };
            parent.Controls.Add(button);
            return button;
        }

        private void SelectAction(PowerAction action)
        {
            if (action == PowerAction.Hibernate && !hibernateAllowed)
            {
                MessageBox.Show(this, Strings.HibernateUnavailable, Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            selectedAction = action;
            for (int i = 0; i < actionButtons.Length; i++)
            {
                actionButtons[i].Selected = i == (int)action;
            }
        }

        private void SelectUnit(int unit)
        {
            selectedUnit = unit;
            for (int i = 0; i < unitButtons.Length; i++)
            {
                unitButtons[i].Selected = i == unit;
            }
            customBox.TextBox.Focus();
            customBox.TextBox.SelectAll();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ---------------- Scheduling ----------------

        private void StartCustom()
        {
            if (!int.TryParse(customBox.TextBox.Text.Trim(), out int value) || value <= 0)
            {
                ShowError(Strings.InvalidNumber);
                customBox.TextBox.Focus();
                return;
            }

            long seconds = (long)value * UnitSeconds[selectedUnit];
            if (seconds > MaxSeconds)
            {
                ShowError(Strings.TooLong);
                customBox.TextBox.Focus();
                return;
            }

            Start((int)seconds);
        }

        private void Start(int seconds)
        {
            PowerAction action = selectedAction;

            if (PowerControl.UsesWindowsScheduler(action))
            {
                Cursor = Cursors.WaitCursor;
                int code = PowerControl.ScheduleWithWindows(action, seconds, Strings.WindowsComment(action));

                if (code == PowerControl.ErrorShutdownIsScheduled)
                {
                    Cursor = Cursors.Default;
                    var answer = MessageBox.Show(this, Strings.AlreadyScheduled, Strings.AppName,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes)
                    {
                        return;
                    }

                    Cursor = Cursors.WaitCursor;
                    PowerControl.AbortWindowsSchedule();
                    code = PowerControl.ScheduleWithWindows(action, seconds, Strings.WindowsComment(action));
                }

                Cursor = Cursors.Default;
                if (code != PowerControl.Success)
                {
                    ShowError(Strings.WindowsRefused(code));
                    return;
                }
            }

            activeAction = action;
            targetUtc = DateTime.UtcNow.AddSeconds(seconds);
            warned = false;
            ShowCountdown();
        }

        private void Snooze()
        {
            if (!running)
            {
                return;
            }

            int remaining = RemainingSeconds();
            int seconds = Math.Min(remaining + SnoozeSeconds, MaxSeconds);

            if (PowerControl.UsesWindowsScheduler(activeAction))
            {
                Cursor = Cursors.WaitCursor;
                PowerControl.AbortWindowsSchedule();
                int code = PowerControl.ScheduleWithWindows(activeAction, seconds, Strings.WindowsComment(activeAction));
                Cursor = Cursors.Default;

                if (code != PowerControl.Success)
                {
                    // The old schedule was already removed, so nothing is pending any more.
                    ShowSetup();
                    ShowError(Strings.WindowsRefused(code));
                    return;
                }
            }

            targetUtc = DateTime.UtcNow.AddSeconds(seconds);
            warned = false;
            UpdateCountdownTexts();
            Tick();
        }

        private void CancelTimer()
        {
            if (running && PowerControl.UsesWindowsScheduler(activeAction))
            {
                Cursor = Cursors.WaitCursor;
                int code = PowerControl.AbortWindowsSchedule();
                Cursor = Cursors.Default;

                if (code != PowerControl.Success && code != PowerControl.ErrorNoShutdownInProgress)
                {
                    ShowError(Strings.WindowsRefused(code));
                    return;
                }
            }

            ShowSetup();
        }

        private void CancelExistingSchedule()
        {
            Cursor = Cursors.WaitCursor;
            int code = PowerControl.AbortWindowsSchedule();
            Cursor = Cursors.Default;

            if (code == PowerControl.Success)
            {
                MessageBox.Show(this, Strings.ExistingCancelled, Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (code == PowerControl.ErrorNoShutdownInProgress)
            {
                MessageBox.Show(this, Strings.NothingToCancel, Strings.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                ShowError(Strings.WindowsRefused(code));
            }
        }

        // ---------------- Countdown ----------------

        private void ShowCountdown()
        {
            running = true;
            setupPanel.Visible = false;
            countdownPanel.Visible = true;
            AcceptButton = null;
            snoozeButton.Enabled = true;
            cancelButton.Enabled = true;
            ActiveControl = null;

            UpdateCountdownTexts();
            Tick();
            ticker.Start();
        }

        private void ShowSetup()
        {
            running = false;
            ticker.Stop();
            countdownPanel.Visible = false;
            setupPanel.Visible = true;
            AcceptButton = scheduleButton;
            Text = Strings.AppName;
        }

        private void UpdateCountdownTexts()
        {
            countdownTitle.Text = Strings.CountdownTitle(activeAction);
            targetLabel.Text = Strings.TargetTime(targetUtc.ToLocalTime());
            hintLabel.Text = PowerControl.UsesWindowsScheduler(activeAction)
                ? Strings.HintWindowsScheduler
                : Strings.HintAppTimer;
        }

        private int RemainingSeconds()
        {
            double seconds = (targetUtc - DateTime.UtcNow).TotalSeconds;
            return seconds <= 0 ? 0 : (int)Math.Ceiling(seconds);
        }

        internal static string FormatDuration(int totalSeconds)
        {
            int hours = totalSeconds / 3600;
            int minutes = totalSeconds / 60 % 60;
            int seconds = totalSeconds % 60;
            return hours > 0
                ? hours + ":" + minutes.ToString("00") + ":" + seconds.ToString("00")
                : minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        private void Tick()
        {
            if (!running)
            {
                return;
            }

            int remaining = RemainingSeconds();
            string clock = FormatDuration(remaining);
            if (countdownLabel.Text != clock)
            {
                countdownLabel.Text = clock;
                Text = clock + " · " + Strings.AppName;
            }

            bool appTimer = !PowerControl.UsesWindowsScheduler(activeAction);

            // Sleep/hibernate: pop the window up a minute before, so it can still be cancelled.
            // (For shutdown/restart Windows shows its own warning.)
            if (appTimer && !warned && remaining <= WarningSeconds)
            {
                warned = true;
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }
                Activate();
            }

            if (remaining > 0)
            {
                return;
            }

            ticker.Stop();
            if (appTimer)
            {
                PowerAction action = activeAction;
                ShowSetup();
                BeginInvoke((MethodInvoker)(() => SuspendNow(action)));
            }
            else
            {
                // Windows is shutting down / restarting now.
                snoozeButton.Enabled = false;
                cancelButton.Enabled = false;
            }
        }

        private void SuspendNow(PowerAction action)
        {
            bool ok;
            try
            {
                ok = PowerControl.SuspendNow(action);
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
            {
                ShowError(Strings.SuspendFailed);
            }
        }

        // ---------------- Window plumbing ----------------

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.UseDarkTitleBar(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ActiveControl = null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Shutdown/restart stay scheduled in Windows after the app closes.
            // Sleep/hibernate only exist inside the app, so ask before throwing the timer away.
            if (running
                && !PowerControl.UsesWindowsScheduler(activeAction)
                && e.CloseReason == CloseReason.UserClosing)
            {
                var answer = MessageBox.Show(this, Strings.CloseCancelsTimer, Strings.AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            ticker.Stop();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.ShowMeMessage && NativeMethods.ShowMeMessage != 0)
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }
                Show();
                Activate();
            }
            base.WndProc(ref m);
        }
    }
}
