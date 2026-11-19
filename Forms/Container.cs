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
        private string currentProfile;
        private bool isShuttingDown;

        public Container()
        {
            this.subject.Attach(this);

            InitializeComponent();
            this.Text = AppConfig.Name + " - " + AppConfig.Version;

            // Container Configuration
            this.IsMdiContainer = true;
            SetBackGroundColorOfMDIForm();

            // Monitor active Ragnarok client so macros stop automatically if the game closes.
            this.clientMonitorTimer = new Timer();
            this.clientMonitorTimer.Interval = 1500;
            this.clientMonitorTimer.Tick += ClientMonitorTimer_Tick;

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
        }

        private void ClientMonitorTimer_Tick(object sender, EventArgs e)
        {
            Client client = ClientSingleton.GetClient();
            if (client != null && !client.IsProcessAlive())
            {
                this.clientMonitorTimer.Stop();
                this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
                this.subject.Notify(new Utils.Message(MessageCode.CLIENT_DISCONNECTED, null));
                ClientSingleton.Clear();
                this.characterName.Text = "- -";
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
                    "This client is not supported. Only Spammers and macro will work.",
                    "Warning",
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
                this.clientMonitorTimer.Stop();
                this.clientMonitorTimer.Dispose();
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
                using (ProfileSingleton.SuppressSave())
                {
                    this.subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                }
                this.currentProfile = selected;
            }
            catch (Exception ex)
            {
                Logger.Error("Container", "Error loading profile '" + selected + "'", ex);
                MessageBox.Show(
                    "Error while loading the new profile.\nDetails: " + ex.Message,
                    "Error",
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
                case MessageCode.PROFILE_CHANGED:
                    UpdateCharacterNameLabel();
                    break;
                case MessageCode.TURN_OFF:
                    this.clientMonitorTimer.Stop();
                    this.profileCB.Enabled = true;
                    this.processCB.Enabled = true;
                    break;
                case MessageCode.TURN_ON:
                    this.profileCB.Enabled = false;
                    this.processCB.Enabled = false;
                    UpdateCharacterNameLabel();
                    this.clientMonitorTimer.Start();
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

        private void UpdateCharacterNameLabel()
        {
            Client client = ClientSingleton.GetClient();
            if (client != null && client.IsProcessAlive())
            {
                string name = client.ReadCharacterName();
                this.characterName.Text = string.IsNullOrWhiteSpace(name) ? "- -" : name;
            }
            else
            {
                this.characterName.Text = "- -";
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