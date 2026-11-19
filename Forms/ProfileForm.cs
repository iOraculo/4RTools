using System;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ProfileForm : Form
    {
        private readonly Container container;

        public ProfileForm(Container container)
        {
            InitializeComponent();
            this.container = container;
            InitializeImportExportButtons();
            RefreshProfileList();
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
    }
}
