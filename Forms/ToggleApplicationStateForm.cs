using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ToggleApplicationStateForm : Form, IObserver
    {
        private readonly Subject subject;
        private ContextMenu contextMenu;
        private MenuItem menuItem;
        private CheckBox cbFocusOnly;
        private ToolTip optionsToolTip;
        private SoundPlayer soundOnPlayer;
        private SoundPlayer soundOffPlayer;

        // Store key used for last profile so it can be cleanly removed when switching profiles.
        private Keys lastKey = Keys.None;
        private bool isUpdatingUi;
        private bool isApplicationOn;

        public ToggleApplicationStateForm(Subject subject)
        {
            InitializeComponent();

            this.subject = subject;
            this.subject.Attach(this);
            KeyboardHook.Enable();

            InitializeAudioPlayers();
            InitializeFocusOnlyCheckbox();
            InitializeContextualMenu();

            this.cbAudio.CheckedChanged += OnAudioCheckedChanged;

            this.txtStatusToggleKey.Text = ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey;
            this.txtStatusToggleKey.KeyDown += new KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusToggleKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusToggleKey.TextChanged += new EventHandler(this.onStatusToggleKeyChange);
        }

        private void InitializeAudioPlayers()
        {
            try
            {
                this.soundOnPlayer = new SoundPlayer(Resources._4RTools.ETCResource.Speech_On);
                this.soundOffPlayer = new SoundPlayer(Resources._4RTools.ETCResource.Speech_Off);
            }
            catch (Exception ex)
            {
                Logger.Warn("ToggleApplicationStateForm", "Failed to initialize sound players", ex);
            }
        }

        private void InitializeFocusOnlyCheckbox()
        {
            this.optionsToolTip = new ToolTip();
            this.cbFocusOnly = new CheckBox
            {
                AutoSize = true,
                Location = new Point(156, 53),
                Name = "cbFocusOnly",
                Text = "Focus",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.optionsToolTip.SetToolTip(this.cbFocusOnly, "Pause automations when the Ragnarok window is not focused");
            this.cbFocusOnly.CheckedChanged += OnFocusOnlyCheckedChanged;
            this.groupBox1.Controls.Add(this.cbFocusOnly);
        }

        private void InitializeContextualMenu()
        {
            this.contextMenu = new ContextMenu();
            this.menuItem = new MenuItem();

            this.contextMenu.MenuItems.AddRange(new MenuItem[] { this.menuItem });

            this.menuItem.Index = 0;
            this.menuItem.Text = "Close";
            this.menuItem.Click += new EventHandler(this.notifyShutdownApplication);

            this.notifyIconTray.ContextMenu = this.contextMenu;
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null)
            {
                return;
            }

            switch (subj.Message.code)
            {
                case MessageCode.PROFILE_CHANGED:
                    this.isUpdatingUi = true;
                    try
                    {
                        UserPreferences prefs = ProfileSingleton.GetCurrent().UserPreferences;
                        Keys currentToggleKey;
                        if (!KeyHelper.TryParseFormsKeys(prefs.toggleStateKey, out currentToggleKey))
                        {
                            currentToggleKey = Keys.End;
                        }

                        KeyboardHook.Remove(this.lastKey);
                        this.txtStatusToggleKey.Text = currentToggleKey.ToString();
                        if (currentToggleKey != Keys.None)
                        {
                            KeyboardHook.Add(currentToggleKey, new KeyboardHook.KeyPressed(this.OnToggleHotkeyPressed));
                        }
                        this.lastKey = currentToggleKey;

                        this.cbAudio.Checked = prefs.soundEnabled;
                        this.cbFocusOnly.Checked = prefs.pauseWhenUnfocused;
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;

                case MessageCode.TURN_OFF:
                    if (this.isApplicationOn)
                    {
                        ApplyOffVisualState("Press the key to start!", Color.Black, false);
                    }
                    break;

                case MessageCode.CLIENT_DISCONNECTED:
                    ApplyOffVisualState("Client closed!", Color.DarkRed, true);
                    break;
            }
        }

        private void btnToggleStatusHandler(object sender, EventArgs e)
        {
            ToggleStatus();
        }

        private bool OnToggleHotkeyPressed()
        {
            // Do not toggle application state while the user is typing a new hotkey.
            if (this.txtStatusToggleKey.Focused)
            {
                return true;
            }

            return ToggleStatus();
        }

        private void onStatusToggleKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Keys currentToggleKey;
            if (!KeyHelper.TryParseFormsKeys(this.txtStatusToggleKey.Text, out currentToggleKey))
            {
                currentToggleKey = Keys.None;
            }

            KeyboardHook.Remove(this.lastKey);
            if (currentToggleKey != Keys.None)
            {
                KeyboardHook.Add(currentToggleKey, new KeyboardHook.KeyPressed(this.OnToggleHotkeyPressed));
            }

            ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey = currentToggleKey.ToString();
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);

            this.lastKey = currentToggleKey;
        }

        private void OnAudioCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().UserPreferences.soundEnabled = this.cbAudio.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
        }

        private void OnFocusOnlyCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().UserPreferences.pauseWhenUnfocused = this.cbFocusOnly.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
        }

        private bool ToggleStatus()
        {
            if (this.isApplicationOn)
            {
                ApplyOffVisualState("Press the key to start!", Color.Black, true);
                this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
            }
            else
            {
                Client client = ClientSingleton.GetClient();
                if (client != null && client.IsProcessAlive())
                {
                    this.isApplicationOn = true;
                    this.btnStatusToggle.BackColor = Color.Green;
                    this.btnStatusToggle.Text = "ON";
                    this.notifyIconTray.Icon = Resources._4RTools.ETCResource.logo_4rtools_on;
                    this.lblStatusToggle.Text = "Press the key to stop!";
                    this.lblStatusToggle.ForeColor = Color.Black;
                    this.subject.Notify(new Utils.Message(MessageCode.TURN_ON, null));

                    PlayAudio(this.soundOnPlayer);
                }
                else
                {
                    this.lblStatusToggle.Text = "Please select the Ragnarok Client!!";
                    this.lblStatusToggle.ForeColor = Color.Red;
                }
            }

            return true;
        }

        private void ApplyOffVisualState(string message, Color textColor, bool playOffSound)
        {
            this.isApplicationOn = false;
            this.btnStatusToggle.BackColor = Color.Red;
            this.btnStatusToggle.Text = "OFF";
            this.notifyIconTray.Icon = Resources._4RTools.ETCResource.logo_4rtools_off;
            this.lblStatusToggle.Text = message;
            this.lblStatusToggle.ForeColor = textColor;

            if (playOffSound)
            {
                PlayAudio(this.soundOffPlayer);
            }
        }

        private void PlayAudio(SoundPlayer player)
        {
            if (!this.cbAudio.Checked || player == null)
            {
                return;
            }

            try
            {
                player.Play();
            }
            catch (Exception ex)
            {
                Logger.Warn("ToggleApplicationStateForm", "Failed to play toggle sound", ex);
            }
        }

        private void notifyIconDoubleClick(object sender, MouseEventArgs e)
        {
            this.subject.Notify(new Utils.Message(MessageCode.CLICK_ICON_TRAY, null));
        }

        private void notifyShutdownApplication(object sender, EventArgs e)
        {
            this.subject.Notify(new Utils.Message(MessageCode.SHUTDOWN_APPLICATION, null));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            KeyboardHook.Remove(this.lastKey);
            if (this.soundOnPlayer != null)
            {
                this.soundOnPlayer.Dispose();
                this.soundOnPlayer = null;
            }
            if (this.soundOffPlayer != null)
            {
                this.soundOffPlayer.Dispose();
                this.soundOffPlayer = null;
            }
            if (this.optionsToolTip != null)
            {
                this.optionsToolTip.Dispose();
                this.optionsToolTip = null;
            }
            if (this.notifyIconTray != null)
            {
                this.notifyIconTray.Visible = false;
            }
            base.OnFormClosed(e);
        }
    }
}