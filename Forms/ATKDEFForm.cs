using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ATKDEFForm : Form, IObserver
    {
        private bool isUpdatingUi;

        public ATKDEFForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            SetupInputs();
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
                    UpdateUI();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AtkDefMode.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AtkDefMode.Stop();
                    break;
            }
        }

        private void UpdateUI()
        {
            this.isUpdatingUi = true;
            try
            {
                ATKDEFMode atkDef = ProfileSingleton.GetCurrent().AtkDefMode;
                this.inSpammerKey.Text = atkDef.keySpammer.ToString();
                FormUtils.SetNumericValue(this.spammerDelay, atkDef.ahkDelay);
                FormUtils.SetNumericValue(this.switchDelay, atkDef.switchDelay);
                this.inSpammerClick.Checked = atkDef.keySpammerWithClick;

                Dictionary<string, Key> atkKeys = atkDef.GetKeysSnapshot(ATKDEFEnum.ATK);
                Dictionary<string, Key> defKeys = atkDef.GetKeysSnapshot(ATKDEFEnum.DEF);

                foreach (Control control in this.panelSwitch.Controls)
                {
                    TextBox tb = control as TextBox;
                    if (tb == null || tb.Tag == null)
                    {
                        continue;
                    }

                    string tagText = tb.Tag.ToString();
                    if (string.Equals(tagText, "spammerKey", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    short parsedMode;
                    if (!short.TryParse(tagText, out parsedMode))
                    {
                        continue;
                    }

                    ATKDEFEnum mode = (ATKDEFEnum)parsedMode;
                    if (mode == ATKDEFEnum.DEF)
                    {
                        tb.Text = defKeys.ContainsKey(tb.Name) ? defKeys[tb.Name].ToString() : Key.None.ToString();
                    }
                    else
                    {
                        tb.Text = atkKeys.ContainsKey(tb.Name) ? atkKeys[tb.Name].ToString() : Key.None.ToString();
                    }
                }
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null)
            {
                return;
            }

            if (delayInput.Name == "spammerDelay")
            {
                ProfileSingleton.GetCurrent().AtkDefMode.ahkDelay = decimal.ToInt32(delayInput.Value);
            }
            else
            {
                ProfileSingleton.GetCurrent().AtkDefMode.switchDelay = decimal.ToInt32(delayInput.Value);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            string tagText = textBox.Tag.ToString();
            if (string.Equals(tagText, "spammerKey", StringComparison.Ordinal))
            {
                ProfileSingleton.GetCurrent().AtkDefMode.keySpammer = key;
            }
            else
            {
                short parsedMode;
                if (!short.TryParse(tagText, out parsedMode))
                {
                    return;
                }

                ATKDEFEnum mode = (ATKDEFEnum)parsedMode;
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem(textBox.Name, key, mode);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().AtkDefMode.keySpammerWithClick = this.inSpammerClick.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        public void SetupInputs()
        {
            foreach (Control c in FormUtils.GetAll(this, typeof(TextBox)))
            {
                TextBox textBox = c as TextBox;
                if (textBox != null)
                {
                    textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                    textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                    textBox.TextChanged += new EventHandler(this.onTextChange);
                }
            }
        }
    }
}
