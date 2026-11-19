using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class Container : Form, IObserver
    {
        private readonly Subject subject = new Subject();
        private readonly Timer clientMonitorTimer;
        private readonly Timer vitalsTimer;
        private readonly byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];
        private AppSettings appSettings;
        private CheckBox cbDarkMode;
        private CheckBox cbTopMost;
        private CheckBox cbMiniHud;
        private ComboBox cbLanguage;
        private VitalBar hpBar;
        private VitalBar spBar;
        private MiniHudForm miniHud;
        private ToolTip headerToolTip;
        private ProfileForm profileForm;
        private string currentProfile;
        private string lastDetectedCharacter = string.Empty;
        private bool isAutomationOn;
        private bool isApplyingSettings;
        private bool isShuttingDown;

        public Container()
        {
            this.subject.Attach(this);
            this.appSettings = AppSettings.Load();
            LocalizationManager.SetLanguage(this.appSettings.Language);

            InitializeComponent();
            this.Text = AppConfig.Name + " - " + AppConfig.Version;

            // Container Configuration
            this.IsMdiContainer = true;
            SetBackGroundColorOfMDIForm();
            InitializeHeaderControlsAndVitals();

            // Monitor active Ragnarok client so macros stop automatically if the game closes.
            this.clientMonitorTimer = new Timer();
            this.clientMonitorTimer.Interval = 1500;
            this.clientMonitorTimer.Tick += ClientMonitorTimer_Tick;

            // Live HP/SP bars and character-change detection for profile auto-switch.
            this.vitalsTimer = new Timer();
            this.vitalsTimer.Interval = 300;
            this.vitalsTimer.Tick += VitalsTimer_Tick;

            ChatPauseState.StateChanged += OnChatPauseStateChanged;

            // Paint Children Forms
            SetToggleApplicationStateWindow();
            SetAutopotWindow();
            SetAutopotYggWindow();
            SetSkillTimerWindow();
            SetProfileWindow();
            SetAHKWindow();
            SetAutobuffSkillWindow();
            SetAutobuffStuffWindow();
            SetDebuffRecoveryWindow();
            SetSongMacroWindow();
            SetATKDEFWindow();
            SetMacroSwitchWindow();
            SetServerWindow();

            ApplyAppSettingsToUi();
        }

        private void InitializeHeaderControlsAndVitals()
        {
            this.headerToolTip = new ToolTip();

            this.cbDarkMode = new CheckBox
            {
                AutoSize = true,
                Location = new Point(226, 12),
                Name = "cbDarkMode",
                Text = "Dark Mode",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbDarkMode.CheckedChanged += OnDarkModeCheckedChanged;

            this.cbMiniHud = new CheckBox
            {
                AutoSize = true,
                Location = new Point(318, 12),
                Name = "cbMiniHud",
                Text = "Mini HUD",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbMiniHud.CheckedChanged += OnMiniHudCheckedChanged;

            this.cbTopMost = new CheckBox
            {
                AutoSize = true,
                Location = new Point(226, 34),
                Name = "cbTopMost",
                Text = "Always on Top",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbTopMost.CheckedChanged += OnTopMostCheckedChanged;

            this.cbLanguage = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new Point(334, 31),
                Name = "cbLanguage",
                Size = new Size(60, 21),
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbLanguage.Items.Add("PT-BR");
            this.cbLanguage.Items.Add("EN");
            this.cbLanguage.SelectedIndexChanged += OnLanguageSelectedIndexChanged;

            this.hpBar = new VitalBar("HP", Color.FromArgb(220, 76, 76))
            {
                Location = new Point(366, 236),
                Name = "hpBar",
                Size = new Size(220, 15)
            };

            this.spBar = new VitalBar("SP", Color.FromArgb(66, 135, 245))
            {
                Location = new Point(366, 253),
                Name = "spBar",
                Size = new Size(220, 15)
            };

            this.Controls.Add(this.cbDarkMode);
            this.Controls.Add(this.cbMiniHud);
            this.Controls.Add(this.cbTopMost);
            this.Controls.Add(this.cbLanguage);
            this.Controls.Add(this.hpBar);
            this.Controls.Add(this.spBar);
        }

        private void ApplyAppSettingsToUi()
        {
            this.isApplyingSettings = true;
            try
            {
                LocalizationManager.SetLanguage(this.appSettings.Language);
                this.cbLanguage.SelectedItem = LocalizationManager.IsPortuguese ? "PT-BR" : "EN";
                this.cbDarkMode.Checked = this.appSettings.DarkMode;
                this.cbTopMost.Checked = this.appSettings.AlwaysOnTop;
                this.cbMiniHud.Checked = this.appSettings.ShowMiniHud;
                this.TopMost = this.appSettings.AlwaysOnTop;
                ApplyLanguageToUi();
                ThemeManager.ApplyTheme(this, this.appSettings.DarkMode);
                SetMiniHudVisible(this.appSettings.ShowMiniHud);
            }
            finally
            {
                this.isApplyingSettings = false;
            }
        }

        private void ApplyLanguageToUi()
        {
            LocalizationManager.ApplyToControlTree(this);
            RefreshHeaderTooltips();
            if (this.profileForm != null)
            {
                this.profileForm.RefreshCharacterBindingUi();
            }
            this.subject.Notify(new Utils.Message(MessageCode.LANGUAGE_CHANGED, LocalizationManager.CurrentLanguage));
        }

        private void RefreshHeaderTooltips()
        {
            if (this.headerToolTip == null)
            {
                return;
            }

            this.headerToolTip.SetToolTip(this.cbDarkMode, LocalizationManager.T("tooltip.dark_mode"));
            this.headerToolTip.SetToolTip(this.cbMiniHud, LocalizationManager.T("tooltip.mini_hud"));
            this.headerToolTip.SetToolTip(this.cbTopMost, LocalizationManager.T("tooltip.top_most"));
            this.headerToolTip.SetToolTip(this.cbLanguage, LocalizationManager.T("tooltip.language"));
        }

        private void OnLanguageSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.isApplyingSettings || this.cbLanguage.SelectedItem == null)
            {
                return;
            }

            string selected = this.cbLanguage.SelectedItem.ToString();
            this.appSettings.Language = string.Equals(selected, "EN", StringComparison.OrdinalIgnoreCase)
                ? LocalizationManager.LANG_EN_US
                : LocalizationManager.LANG_PT_BR;

            LocalizationManager.SetLanguage(this.appSettings.Language);
            ApplyLanguageToUi();
            AppSettings.Save(this.appSettings);
        }

        public void ReloadAppSettings()
        {
            this.appSettings = AppSettings.Load();
            ApplyAppSettingsToUi();
            if (this.profileForm != null)
            {
                this.profileForm.RefreshCharacterBindingUi();
            }
        }

        public void NotifyServersRestored()
        {
            this.subject.Notify(new Utils.Message(MessageCode.SERVER_LIST_CHANGED, "Servers Restored"));
        }

        private void OnDarkModeCheckedChanged(object sender, EventArgs e)
        {
            if (this.isApplyingSettings)
            {
                return;
            }

            this.appSettings.DarkMode = this.cbDarkMode.Checked;
            ThemeManager.ApplyTheme(this, this.appSettings.DarkMode);
            if (this.miniHud != null && !this.miniHud.IsDisposed)
            {
                this.miniHud.ApplyHudTheme(this.appSettings.DarkMode);
            }
            AppSettings.Save(this.appSettings);
        }

        private void OnTopMostCheckedChanged(object sender, EventArgs e)
        {
            if (this.isApplyingSettings)
            {
                return;
            }

            this.appSettings.AlwaysOnTop = this.cbTopMost.Checked;
            this.TopMost = this.appSettings.AlwaysOnTop;
            AppSettings.Save(this.appSettings);
        }

        private void OnMiniHudCheckedChanged(object sender, EventArgs e)
        {
            if (this.isApplyingSettings)
            {
                return;
            }

            this.appSettings.ShowMiniHud = this.cbMiniHud.Checked;
            SetMiniHudVisible(this.appSettings.ShowMiniHud);
            AppSettings.Save(this.appSettings);
        }

        private void SetMiniHudVisible(bool visible)
        {
            if (visible)
            {
                if (this.miniHud == null || this.miniHud.IsDisposed)
                {
                    this.miniHud = new MiniHudForm();
                    this.miniHud.CloseRequested += OnMiniHudCloseRequested;
                    this.miniHud.PositionChanged += OnMiniHudPositionChanged;
                    this.miniHud.SetSavedPosition(this.appSettings.MiniHudX, this.appSettings.MiniHudY);
                }

                this.miniHud.ApplyHudTheme(this.appSettings.DarkMode);
                this.miniHud.UpdateStatus(this.isAutomationOn, ChatPauseState.IsChatting);
                this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
                if (!this.miniHud.Visible)
                {
                    this.miniHud.Show();
                }

                UpdateVitalsDisplay(ClientSingleton.GetClient());
            }
            else if (this.miniHud != null && !this.miniHud.IsDisposed && this.miniHud.Visible)
            {
                this.miniHud.Hide();
            }
        }

        private void OnMiniHudCloseRequested(object sender, EventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.cbMiniHud.Checked = false;
        }

        private void OnMiniHudPositionChanged(object sender, EventArgs e)
        {
            if (this.miniHud == null || this.miniHud.IsDisposed || this.appSettings == null)
            {
                return;
            }

            this.appSettings.MiniHudX = this.miniHud.Location.X;
            this.appSettings.MiniHudY = this.miniHud.Location.Y;
            AppSettings.Save(this.appSettings);
        }

        private void OnChatPauseStateChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            MethodInvoker updateHud = delegate
            {
                if (this.miniHud != null && !this.miniHud.IsDisposed)
                {
                    this.miniHud.UpdateStatus(this.isAutomationOn, ChatPauseState.IsChatting);
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(updateHud);
            }
            else
            {
                updateHud();
            }
        }

        public bool GetAutoSwitchByCharacter()
        {
            return this.appSettings != null && this.appSettings.AutoSwitchProfileByCharacter;
        }

        public void SetAutoSwitchByCharacter(bool enabled)
        {
            if (this.appSettings == null)
            {
                return;
            }

            this.appSettings.AutoSwitchProfileByCharacter = enabled;
            AppSettings.Save(this.appSettings);
        }

        public void TryAutoSwitchProfileForCharacter(string charName)
        {
            if (this.appSettings == null || !this.appSettings.AutoSwitchProfileByCharacter)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(charName) || !this.profileCB.Enabled)
            {
                return;
            }

            string matchedProfile = ProfileSingleton.FindProfileForCharacter(charName);
            if (string.IsNullOrEmpty(matchedProfile)
                || string.Equals(matchedProfile, this.currentProfile, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!this.profileCB.Items.Contains(matchedProfile))
            {
                this.refreshProfileList();
            }

            if (this.profileCB.Items.Contains(matchedProfile))
            {
                this.profileCB.SelectedItem = matchedProfile;
            }
        }

        private void VitalsTimer_Tick(object sender, EventArgs e)
        {
            if (this.isShuttingDown)
            {
                return;
            }

            Client client = ClientSingleton.GetClient();
            if (client == null || !client.IsProcessAlive())
            {
                this.vitalsTimer.Stop();
                this.hpBar.ClearValues();
                this.spBar.ClearValues();
                if (this.miniHud != null && !this.miniHud.IsDisposed)
                {
                    this.miniHud.ClearVitals();
                }
                this.lastDetectedCharacter = string.Empty;
                return;
            }

            UpdateVitalsDisplay(client);

            if (client.HasMemorySupport)
            {
                string detected = client.ReadCharacterName();
                string normalized = string.IsNullOrWhiteSpace(detected) ? string.Empty : detected.Trim();
                if (!string.Equals(normalized, this.lastDetectedCharacter, StringComparison.Ordinal))
                {
                    this.lastDetectedCharacter = normalized;
                    this.characterName.Text = string.IsNullOrEmpty(normalized) ? "- -" : normalized;
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
                    }

                    if (!string.IsNullOrEmpty(normalized) && this.profileCB.Enabled)
                    {
                        TryAutoSwitchProfileForCharacter(normalized);
                    }
                }
            }
        }

        private void UpdateVitalsDisplay(Client client)
        {
            if (client != null && client.HasMemorySupport && client.IsProcessAlive())
            {
                uint currentHp;
                uint maxHp;
                uint currentSp;
                uint maxSp;
                if (client.TryReadVitals(this.vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp))
                {
                    this.hpBar.UpdateValues(currentHp, maxHp);
                    this.spBar.UpdateValues(currentSp, maxSp);
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.UpdateVitals(currentHp, maxHp, currentSp, maxSp);
                    }
                    return;
                }
            }

            this.hpBar.ClearValues();
            this.spBar.ClearValues();
            if (this.miniHud != null && !this.miniHud.IsDisposed)
            {
                this.miniHud.ClearVitals();
            }
        }

        private void ClientMonitorTimer_Tick(object sender, EventArgs e)
        {
            Client client = ClientSingleton.GetClient();
            if (client != null && !client.IsProcessAlive())
            {
                this.clientMonitorTimer.Stop();
                this.vitalsTimer.Stop();
                this.isAutomationOn = false;
                this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
                this.subject.Notify(new Utils.Message(MessageCode.CLIENT_DISCONNECTED, null));
                ClientSingleton.Clear();
                this.lastDetectedCharacter = string.Empty;
                this.characterName.Text = "- -";
                this.hpBar.ClearValues();
                this.spBar.ClearValues();
                if (this.miniHud != null && !this.miniHud.IsDisposed)
                {
                    this.miniHud.ClearVitals();
                    this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, "- -");
                    this.miniHud.UpdateStatus(false, false);
                }
                this.refreshProcessList();
            }
        }

        public void addform(TabPage tp, Form f)
        {
            if (!tp.Controls.Contains(f))
            {
                tp.Controls.Add(f);
                f.Dock = DockStyle.Fill;
                f.Show();
            }
            Refresh();
        }

        private void SetBackGroundColorOfMDIForm()
        {
            foreach (Control ctl in this.Controls)
            {
                if (ctl is MdiClient)
                {
                    ctl.BackColor = Color.White;
                }
            }
        }

        private void processCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.processCB.SelectedItem == null)
            {
                return;
            }

            Client client = new Client(this.processCB.SelectedItem.ToString());
            ClientSingleton.Instance(client);

            if (!client.IsSupported)
            {
                MessageBox.Show(
                    LocalizationManager.T("msg.client_unsupported"),
                    LocalizationManager.T("title.warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            this.subject.Notify(new Utils.Message(Utils.MessageCode.PROCESS_CHANGED, null));
        }

        private void Container_Load(object sender, EventArgs e)
        {
            ProfileSingleton.Create("Default");
            this.refreshProcessList();
            this.refreshProfileList("Default");
            ApplyLanguageToUi();
            ThemeManager.ApplyTheme(this, this.appSettings.DarkMode);
        }

        public void refreshProfileList(string selectProfile = null)
        {
            MethodInvoker updateAction = delegate
            {
                string previousSelection = selectProfile ?? (this.profileCB.SelectedItem as string) ?? this.currentProfile;
                this.profileCB.Items.Clear();

                foreach (string p in Profile.ListAll())
                {
                    this.profileCB.Items.Add(p);
                }

                if (!string.IsNullOrEmpty(previousSelection) && this.profileCB.Items.Contains(previousSelection))
                {
                    this.profileCB.SelectedItem = previousSelection;
                }
                else if (this.profileCB.Items.Contains("Default"))
                {
                    this.profileCB.SelectedItem = "Default";
                }
            };

            if (this.InvokeRequired)
            {
                this.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        public void OnProfileRenamed(string oldName, string newName)
        {
            if (string.Equals(this.currentProfile, oldName, StringComparison.OrdinalIgnoreCase))
            {
                this.currentProfile = newName;
                this.refreshProfileList(newName);
            }
            else
            {
                this.refreshProfileList();
            }
        }

        public void OnProfileDeleted(string deletedName)
        {
            if (string.Equals(this.currentProfile, deletedName, StringComparison.OrdinalIgnoreCase))
            {
                this.currentProfile = null;
                this.refreshProfileList("Default");
            }
            else
            {
                this.refreshProfileList();
            }
        }

        private void refreshProcessList()
        {
            MethodInvoker updateAction = delegate
            {
                string previousSelection = this.processCB.SelectedItem as string;
                this.processCB.Items.Clear();

                Process[] allProcesses = Process.GetProcesses();
                foreach (Process p in allProcesses)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(p.MainWindowTitle) && ClientListSingleton.ExistsByProcessName(p.ProcessName))
                        {
                            this.processCB.Items.Add(string.Format("{0}.exe - {1}", p.ProcessName, p.Id));
                        }
                    }
                    catch
                    {
                        // Ignore inaccessible system processes.
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }

                if (!string.IsNullOrEmpty(previousSelection) && this.processCB.Items.Contains(previousSelection))
                {
                    this.processCB.SelectedItem = previousSelection;
                }
            };

            if (this.InvokeRequired)
            {
                this.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            this.refreshProcessList();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ShutdownApplication(false);
            base.OnFormClosing(e);
        }

        private void ShutdownApplication(bool requestClose)
        {
            if (this.isShuttingDown)
            {
                return;
            }

            this.isShuttingDown = true;

            try
            {
                ChatPauseState.StateChanged -= OnChatPauseStateChanged;
                this.clientMonitorTimer.Stop();
                this.clientMonitorTimer.Dispose();
                this.vitalsTimer.Stop();
                this.vitalsTimer.Dispose();
                if (this.miniHud != null && !this.miniHud.IsDisposed)
                {
                    this.miniHud.CloseRequested -= OnMiniHudCloseRequested;
                    this.miniHud.PositionChanged -= OnMiniHudPositionChanged;
                    this.miniHud.Close();
                    this.miniHud.Dispose();
                    this.miniHud = null;
                }
                if (this.headerToolTip != null)
                {
                    this.headerToolTip.Dispose();
                    this.headerToolTip = null;
                }
                ChatPauseState.Resume();
                KeyboardHook.Disable();
                this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
                ClientSingleton.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error("Container", "Error during shutdown", ex);
            }

            if (requestClose && !this.IsDisposed)
            {
                this.Close();
            }
        }

        private void lblLinkGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.GithubLink);
        }

        private void lblLinkDiscord_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.DiscordLink);
        }

        private void websiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.Website);
        }

        private static void OpenExternalUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Warn("Container", "Failed to open URL: " + url, ex);
            }
        }

        public void ReloadCurrentProfile()
        {
            string active = !string.IsNullOrWhiteSpace(this.currentProfile)
                ? this.currentProfile
                : ProfileSingleton.GetCurrent().Name;

            if (string.IsNullOrWhiteSpace(active))
            {
                return;
            }

            try
            {
                ProfileSingleton.Load(active);
                using (ProfileSingleton.SuppressSave())
                {
                    this.subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Container", "Failed to reload active profile '" + active + "'", ex);
            }
        }

        private void profileCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = this.profileCB.Text;
            if (string.IsNullOrWhiteSpace(selected) || string.Equals(selected, this.currentProfile, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                ProfileSingleton.Load(selected);
                this.currentProfile = selected;
                using (ProfileSingleton.SuppressSave())
                {
                    this.subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Container", "Error loading profile '" + selected + "'", ex);
                MessageBox.Show(
                    LocalizationManager.T("msg.profile_load_error", ex.Message),
                    LocalizationManager.T("title.error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
                case MessageCode.PROCESS_CHANGED:
                    Client activeClient = ClientSingleton.GetClient();
                    UpdateCharacterNameLabel(true);
                    UpdateVitalsDisplay(activeClient);
                    if (activeClient != null && activeClient.IsProcessAlive())
                    {
                        this.vitalsTimer.Start();
                    }
                    else
                    {
                        this.vitalsTimer.Stop();
                    }
                    break;
                case MessageCode.PROFILE_CHANGED:
                    UpdateCharacterNameLabel(false);
                    if (this.profileForm != null)
                    {
                        this.profileForm.RefreshCharacterBindingUi();
                    }
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
                    }
                    break;
                case MessageCode.TURN_OFF:
                    this.isAutomationOn = false;
                    this.clientMonitorTimer.Stop();
                    this.profileCB.Enabled = true;
                    this.processCB.Enabled = true;
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.UpdateStatus(false, false);
                    }
                    break;
                case MessageCode.TURN_ON:
                    this.isAutomationOn = true;
                    this.profileCB.Enabled = false;
                    this.processCB.Enabled = false;
                    UpdateCharacterNameLabel(false);
                    this.clientMonitorTimer.Start();
                    this.vitalsTimer.Start();
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.UpdateStatus(true, ChatPauseState.IsChatting);
                    }
                    break;
                case MessageCode.SERVER_LIST_CHANGED:
                    this.refreshProcessList();
                    break;
                case MessageCode.CLICK_ICON_TRAY:
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                    break;
                case MessageCode.SHUTDOWN_APPLICATION:
                    ShutdownApplication(true);
                    break;
            }
        }

        private void UpdateCharacterNameLabel(bool allowAutoSwitch)
        {
            Client client = ClientSingleton.GetClient();
            if (client != null && client.IsProcessAlive())
            {
                string name = client.ReadCharacterName();
                string normalized = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
                this.lastDetectedCharacter = normalized;
                this.characterName.Text = string.IsNullOrEmpty(normalized) ? "- -" : normalized;

                if (allowAutoSwitch && !string.IsNullOrEmpty(normalized))
                {
                    TryAutoSwitchProfileForCharacter(normalized);
                }
            }
            else
            {
                this.lastDetectedCharacter = string.Empty;
                this.characterName.Text = "- -";
            }

            if (this.miniHud != null && !this.miniHud.IsDisposed)
            {
                this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
            }
        }

        private void containerResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        #region Frames

        public void SetToggleApplicationStateWindow()
        {
            ToggleApplicationStateForm frm = new ToggleApplicationStateForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            this.OnOffPanel.Controls.Add(frm);
            frm.Show();
        }

        public void SetAutopotWindow()
        {
            AutopotForm frm = new AutopotForm(this.subject, false);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageAutopot, frm);
        }

        public void SetAutopotYggWindow()
        {
            AutopotForm frm = new AutopotForm(this.subject, true);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageYggAutopot, frm);
        }

        public void SetSkillTimerWindow()
        {
            SkillTimerForm frm = new SkillTimerForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabSkillTimer, frm);
        }

        public void SetProfileWindow()
        {
            ProfileForm frm = new ProfileForm(this);
            this.profileForm = frm;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageProfiles, frm);
        }

        public void SetServerWindow()
        {
            ServersForm frm = new ServersForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageServer, frm);
        }

        public void SetAHKWindow()
        {
            AHKForm frm = new AHKForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageSpammer, frm);
        }

        public void SetAutobuffSkillWindow()
        {
            SkillAutoBuffForm frm = new SkillAutoBuffForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.tabPageAutobuffSkill, frm);
            frm.Show();
        }

        public void SetAutobuffStuffWindow()
        {
            StuffAutoBuffForm frm = new StuffAutoBuffForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageAutobuffStuff, frm);
        }

        public void SetDebuffRecoveryWindow()
        {
            DebuffRecoveryForm frm = new DebuffRecoveryForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabDebuffRecovery, frm);
        }

        public void SetSongMacroWindow()
        {
            MacroSongForm frm = new MacroSongForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.tabPageMacroSongs, frm);
            frm.Show();
        }

        public void SetATKDEFWindow()
        {
            ATKDEFForm frm = new ATKDEFForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.atkDef, frm);
            frm.Show();
        }

        public void SetMacroSwitchWindow()
        {
            MacroSwitchForm frm = new MacroSwitchForm(this.subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.tabMacroSwitch, frm);
            frm.Show();
        }
        #endregion
    }
}