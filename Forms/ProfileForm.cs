using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ProfileForm : Form
    {
        private readonly Container container;
        private CheckBox cbAutoSwitch;
        private Label lblTargetProfile;
        private TextBox txtBoundCharacter;
        private bool isUpdatingBindingUi;

        public ProfileForm(Container container)
        {
            InitializeComponent();
            this.container = container;
            InitializeImportExportButtons();
            InitializeCharacterBindingPanel();
            InitializeBackupAndDiagnosticsBar();
            this.lbProfilesList.SelectedIndexChanged += lbProfilesList_SelectedIndexChanged;
            RefreshProfileList();
            RefreshCharacterBindingUi();
        }

        private void InitializeImportExportButtons()
        {
            Button btnExport = new Button
            {
                Location = new Point(267, 164),
                Name = "btnExportProfile",
                Size = new Size(78, 23),
                Text = "Export",
                UseVisualStyleBackColor = true
            };
            btnExport.Click += btnExport_Click;

            Button btnImport = new Button
            {
                Location = new Point(267, 193),
                Name = "btnImportProfile",
                Size = new Size(78, 23),
                Text = "Import",
                UseVisualStyleBackColor = true
            };
            btnImport.Click += btnImport_Click;

            this.Controls.Add(btnExport);
            this.Controls.Add(btnImport);
        }

        private void InitializeBackupAndDiagnosticsBar()
        {
            Button btnBackupZip = new Button
            {
                Location = new Point(23, 224),
                Name = "btnBackupZip",
                Size = new Size(115, 25),
                Text = "Backup All (.zip)",
                UseVisualStyleBackColor = true
            };
            btnBackupZip.Click += btnBackupZip_Click;

            Button btnRestoreZip = new Button
            {
                Location = new Point(146, 224),
                Name = "btnRestoreZip",
                Size = new Size(115, 25),
                Text = "Restore (.zip)",
                UseVisualStyleBackColor = true
            };
            btnRestoreZip.Click += btnRestoreZip_Click;

            Button btnCheckConflicts = new Button
            {
                Location = new Point(267, 224),
                Name = "btnCheckConflicts",
                Size = new Size(140, 25),
                Text = "Check Key Conflicts",
                UseVisualStyleBackColor = true
            };
            btnCheckConflicts.Click += btnCheckConflicts_Click;

            this.Controls.Add(btnBackupZip);
            this.Controls.Add(btnRestoreZip);
            this.Controls.Add(btnCheckConflicts);
        }

        private void InitializeCharacterBindingPanel()
        {
            GroupBox gbAutoSwitch = new GroupBox
            {
                Location = new Point(358, 13),
                Name = "gbAutoSwitchCharacter",
                Size = new Size(190, 204),
                TabStop = false,
                Text = "Auto-Switch by Character"
            };

            this.cbAutoSwitch = new CheckBox
            {
                AutoSize = true,
                Location = new Point(10, 20),
                Name = "cbAutoSwitch",
                Text = "Enable Auto-Switch",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbAutoSwitch.CheckedChanged += cbAutoSwitch_CheckedChanged;

            this.lblTargetProfile = new Label
            {
                Location = new Point(10, 44),
                Name = "lblTargetProfile",
                Size = new Size(170, 15),
                Text = "Profile: Default"
            };

            Label lblBoundChar = new Label
            {
                AutoSize = true,
                Location = new Point(10, 64),
                Name = "lblBoundChar",
                Text = "Character Name:"
            };

            this.txtBoundCharacter = new TextBox
            {
                Location = new Point(10, 81),
                Name = "txtBoundCharacter",
                Size = new Size(168, 20),
                Tag = FormUtils.SKIP_RESET_TAG
            };

            Button btnUseActiveChar = new Button
            {
                Location = new Point(10, 108),
                Name = "btnUseActiveChar",
                Size = new Size(168, 23),
                Text = "Use Active Character",
                UseVisualStyleBackColor = true
            };
            btnUseActiveChar.Click += btnUseActiveChar_Click;

            Button btnSaveBinding = new Button
            {
                Location = new Point(10, 137),
                Name = "btnSaveBinding",
                Size = new Size(80, 23),
                Text = "Save",
                UseVisualStyleBackColor = true
            };
            btnSaveBinding.Click += btnSaveBinding_Click;

            Button btnClearBinding = new Button
            {
                Location = new Point(98, 137),
                Name = "btnClearBinding",
                Size = new Size(80, 23),
                Text = "Clear",
                UseVisualStyleBackColor = true
            };
            btnClearBinding.Click += btnClearBinding_Click;

            Label lblBindingHint = new Label
            {
                Font = new Font("Microsoft Sans Serif", 7.5F),
                Location = new Point(10, 166),
                Name = "lblBindingHint",
                Size = new Size(170, 32),
                Text = "Auto-loads profile when this character is detected (while OFF)."
            };

            gbAutoSwitch.Controls.Add(this.cbAutoSwitch);
            gbAutoSwitch.Controls.Add(this.lblTargetProfile);
            gbAutoSwitch.Controls.Add(lblBoundChar);
            gbAutoSwitch.Controls.Add(this.txtBoundCharacter);
            gbAutoSwitch.Controls.Add(btnUseActiveChar);
            gbAutoSwitch.Controls.Add(btnSaveBinding);
            gbAutoSwitch.Controls.Add(btnClearBinding);
            gbAutoSwitch.Controls.Add(lblBindingHint);

            this.Controls.Add(gbAutoSwitch);
        }

        private string GetTargetProfileForBinding()
        {
            if (this.lbProfilesList.SelectedItem != null)
            {
                return this.lbProfilesList.SelectedItem.ToString();
            }

            Profile current = ProfileSingleton.GetCurrent();
            return current != null ? current.Name : "Default";
        }

        public void RefreshCharacterBindingUi()
        {
            if (this.txtBoundCharacter == null || this.lblTargetProfile == null || this.cbAutoSwitch == null)
            {
                return;
            }

            this.isUpdatingBindingUi = true;
            try
            {
                string targetProfile = GetTargetProfileForBinding();
                this.cbAutoSwitch.Checked = this.container != null && this.container.GetAutoSwitchByCharacter();
                this.lblTargetProfile.Text = "Profile: " + targetProfile;
                this.txtBoundCharacter.Text = ProfileSingleton.GetBoundCharacter(targetProfile);
            }
            finally
            {
                this.isUpdatingBindingUi = false;
            }
        }

        private void lbProfilesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshCharacterBindingUi();
        }

        private void cbAutoSwitch_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingBindingUi || this.container == null)
            {
                return;
            }

            this.container.SetAutoSwitchByCharacter(this.cbAutoSwitch.Checked);
        }

        private void btnUseActiveChar_Click(object sender, EventArgs e)
        {
            Client client = ClientSingleton.GetClient();
            if (client == null || !client.IsProcessAlive() || !client.HasMemorySupport)
            {
                MessageBox.Show(
                    "Select a supported Ragnarok client first to read the active character name.",
                    "Active Character",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string name = client.ReadCharacterName();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Could not detect an active character name from the Ragnarok client.",
                    "Active Character",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            this.txtBoundCharacter.Text = name.Trim();
        }

        private void btnSaveBinding_Click(object sender, EventArgs e)
        {
            string targetProfile = GetTargetProfileForBinding();
            try
            {
                string charName = this.txtBoundCharacter.Text != null ? this.txtBoundCharacter.Text.Trim() : string.Empty;
                ProfileSingleton.SetBoundCharacter(targetProfile, charName);
                RefreshCharacterBindingUi();

                if (!string.IsNullOrEmpty(charName) && this.container != null)
                {
                    Client client = ClientSingleton.GetClient();
                    if (client != null && client.IsProcessAlive() && client.HasMemorySupport)
                    {
                        string activeChar = client.ReadCharacterName();
                        if (!string.IsNullOrWhiteSpace(activeChar))
                        {
                            this.container.TryAutoSwitchProfileForCharacter(activeChar.Trim());
                        }
                    }
                }

                string msg = string.IsNullOrEmpty(charName)
                    ? "Character binding cleared for profile '" + targetProfile + "'."
                    : "Profile '" + targetProfile + "' bound to character '" + charName + "'.";
                MessageBox.Show(msg, "Character Binding", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.Warn("ProfileForm", "Failed to save character binding", ex);
                MessageBox.Show("Failed to save character binding: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearBinding_Click(object sender, EventArgs e)
        {
            this.txtBoundCharacter.Text = string.Empty;
            btnSaveBinding_Click(sender, e);
        }

        private void RefreshProfileList()
        {
            string selected = this.lbProfilesList.SelectedItem as string;
            this.lbProfilesList.Items.Clear();

            foreach (string profile in Profile.ListAll())
            {
                if (!string.Equals(profile, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    this.lbProfilesList.Items.Add(profile);
                }
            }

            if (!string.IsNullOrEmpty(selected) && this.lbProfilesList.Items.Contains(selected))
            {
                this.lbProfilesList.SelectedItem = selected;
            }

            RefreshCharacterBindingUi();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string newProfileName = this.txtProfileName.Text;
            if (string.IsNullOrWhiteSpace(newProfileName))
            {
                return;
            }

            try
            {
                string validName = ProfileSingleton.ValidateProfileName(newProfileName);
                ProfileSingleton.Create(validName);
                RefreshProfileList();
                this.container.refreshProfileList(validName);
                this.txtProfileName.Text = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Warn("ProfileForm", "Failed to create profile", ex);
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRemoveProfile_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("No profile selected! To delete a profile, first select an option from the Profile list.");
                return;
            }

            string selectedProfile = this.lbProfilesList.SelectedItem.ToString();
            if (string.Equals(selectedProfile, "Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot delete the Default profile!");
                return;
            }

            ProfileSingleton.Delete(selectedProfile);
            RefreshProfileList();
            this.container.OnProfileDeleted(selectedProfile);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("To rename a profile, first select one from the Profile list.");
                return;
            }

            string selectedProfile = this.lbProfilesList.SelectedItem.ToString();
            if (string.Equals(selectedProfile, "Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot rename the Default profile!");
                return;
            }

            using (EditProfileName editProfileName = new EditProfileName())
            {
                editProfileName.SetProfileName(selectedProfile);
                ThemeManager.ApplyCurrentTheme(editProfileName);
                if (editProfileName.ShowDialog() == DialogResult.OK)
                {
                    RefreshProfileList();
                    this.container.OnProfileRenamed(selectedProfile, editProfileName.NewProfileName);
                }
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("To copy a profile, first select one from the Profile list.");
                return;
            }

            string selectedProfile = this.lbProfilesList.SelectedItem.ToString();
            if (string.Equals(selectedProfile, "Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot copy the Default profile!");
                return;
            }

            try
            {
                ProfileSingleton.Copy(selectedProfile);
                RefreshProfileList();
                this.container.refreshProfileList();
            }
            catch (Exception ex)
            {
                Logger.Warn("ProfileForm", "Failed to copy profile", ex);
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            string targetProfile = this.lbProfilesList.SelectedItem != null
                ? this.lbProfilesList.SelectedItem.ToString()
                : ProfileSingleton.GetCurrent().Name;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "4RTools Profile (*.json)|*.json";
                dialog.FileName = targetProfile + ".json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ProfileSingleton.Export(targetProfile, dialog.FileName);
                        MessageBox.Show("Profile exported successfully!", "Export Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to export profile", ex);
                        MessageBox.Show("Failed to export profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "4RTools Profile (*.json)|*.json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string importedName = ProfileSingleton.Import(dialog.FileName);
                        RefreshProfileList();
                        this.container.refreshProfileList();
                        MessageBox.Show("Profile '" + importedName + "' imported successfully!", "Import Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to import profile", ex);
                        MessageBox.Show("Failed to import profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnBackupZip_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "4RTools Backup (*.zip)|*.zip";
                dialog.FileName = "4RTools-Backup-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".zip";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        int count = ProfileBackupManager.ExportBackupZip(dialog.FileName);
                        MessageBox.Show(
                            string.Format("Backup created successfully ({0} file(s) exported)!", count),
                            "Backup All",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to export full backup", ex);
                        MessageBox.Show("Failed to create backup: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnRestoreZip_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "4RTools Backup (*.zip)|*.zip";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (MessageBox.Show(
                            "Restore profiles, custom servers, and settings from this backup?\nExisting profiles with the same name will be updated.",
                            "Restore Backup",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    try
                    {
                        BackupRestoreResult result = ProfileBackupManager.ImportBackupZip(dialog.FileName);
                        RefreshProfileList();
                        this.container.refreshProfileList();
                        this.container.ReloadCurrentProfile();

                        if (result.ServersRestored)
                        {
                            this.container.NotifyServersRestored();
                        }
                        if (result.SettingsRestored)
                        {
                            this.container.ReloadAppSettings();
                        }

                        string summary = string.Format(
                            "Backup restored!\nProfiles restored: {0}\nCustom servers restored: {1}\nApp settings restored: {2}",
                            result.ProfilesRestored,
                            result.ServersRestored ? "Yes" : "No",
                            result.SettingsRestored ? "Yes" : "No");
                        MessageBox.Show(summary, "Restore Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("ProfileForm", "Failed to restore backup", ex);
                        MessageBox.Show("Failed to restore backup: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnCheckConflicts_Click(object sender, EventArgs e)
        {
            Profile current = ProfileSingleton.GetCurrent();
            List<KeybindConflict> conflicts = KeybindConflictDetector.DetectConflicts(current);
            string report = KeybindConflictDetector.FormatReport(current != null ? current.Name : "Default", conflicts);
            MessageBoxIcon icon = conflicts.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            MessageBox.Show(report, "Keybind Conflict Inspector", MessageBoxButtons.OK, icon);
        }
    }
}
