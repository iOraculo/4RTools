using System;
using System.Windows.Forms;
using _4RTools.Model;

namespace _4RTools.Forms
{
    public partial class EditProfileName : Form
    {
        private string tmpProfileName;
        public string NewProfileName { get; private set; }

        public EditProfileName()
        {
            InitializeComponent();
        }

        public void SetProfileName(string name)
        {
            this.txtProfileName.Text = name;
            this.tmpProfileName = name;
        }

        private void onTextChange(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                this.btnSave.Enabled = !string.IsNullOrWhiteSpace(textBox.Text);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string validNewName = ProfileSingleton.ValidateProfileName(this.txtProfileName.Text);
                ProfileSingleton.Rename(this.tmpProfileName, validNewName);
                this.NewProfileName = validNewName;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("[Edit Profile Name] " + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
