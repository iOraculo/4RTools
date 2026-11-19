using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AHKForm : Form, IObserver
    {
        private bool isUpdatingUi;

        public AHKForm(Subject subject)
        {
            InitializeComponent();
            InitializeCheckAsThreeState();
            subject.Attach(this);
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
                        RemoveHandlers();
                        FormUtils.ResetForm(this);
                        SetLegendDefaultValues();
                        InitializeCheckAsThreeState();

                        AHK ahk = ProfileSingleton.GetCurrent().AHK;
                        RadioButton rdAhkMode = this.groupAhkConfig.Controls[ahk.ahkMode] as RadioButton;
                        if (rdAhkMode != null)
                        {
                            rdAhkMode.Checked = true;
                        }

                        FormUtils.SetNumericValue(this.txtSpammerDelay, ahk.AhkDelay);
                        this.chkNoShift.Checked = ahk.noShift;
                        this.chkMouseFlick.Checked = ahk.mouseFlick;
                        DisableControlsIfSpeedBoost();

                        Dictionary<string, KeyConfig> ahkClones = ahk.GetEntriesSnapshot();
                        foreach (KeyValuePair<string, KeyConfig> config in ahkClones)
                        {
                            ToggleCheckboxByName(config.Key, config.Value.ClickActive);
                        }
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AHK.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AHK.Stop();
                    break;
            }
        }

        private void onCheckChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox checkbox = sender as CheckBox;
            if (checkbox == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(checkbox.Text, out key))
            {
                return;
            }

            bool haveMouseClick = checkbox.CheckState == CheckState.Checked;

            if (checkbox.CheckState == CheckState.Checked || checkbox.CheckState == CheckState.Indeterminate)
            {
                ProfileSingleton.GetCurrent().AHK.AddAHKEntry(checkbox.Name, new KeyConfig(key, haveMouseClick));
            }
            else
            {
                ProfileSingleton.GetCurrent().AHK.RemoveAHKEntry(checkbox.Name);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
        }

        private void txtSpammerDelay_TextChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            try
            {
                ProfileSingleton.GetCurrent().AHK.AhkDelay = Math.Max(1, Convert.ToInt32(this.txtSpammerDelay.Value));
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
            }
            catch (Exception ex)
            {
                Logger.Warn("AHKForm", "Invalid spammer delay value", ex);
            }
        }

        private void ToggleCheckboxByName(string name, bool state)
        {
            Control[] matches = this.Controls.Find(name, true);
            if (matches.Length > 0)
            {
                CheckBox checkBox = matches[0] as CheckBox;
                if (checkBox != null)
                {
                    checkBox.CheckState = state ? CheckState.Checked : CheckState.Indeterminate;
                }
            }
        }

        private void RemoveHandlers()
        {
            foreach (Control c in this.Controls)
            {
                CheckBox check = c as CheckBox;
                if (check != null)
                {
                    check.CheckStateChanged -= onCheckChange;
                }
            }
        }

        private void InitializeCheckAsThreeState()
        {
            foreach (Control c in this.Controls)
            {
                CheckBox check = c as CheckBox;
                if (check != null)
                {
                    if (check.Name.StartsWith("chk", StringComparison.Ordinal))
                    {
                        check.ThreeState = true;
                    }

                    if (check.Enabled)
                    {
                        check.CheckStateChanged -= onCheckChange;
                        check.CheckStateChanged += onCheckChange;
                    }
                }
            }
        }

        private void SetLegendDefaultValues()
        {
            this.cbWithNoClick.ThreeState = true;
            this.cbWithNoClick.CheckState = CheckState.Indeterminate;
            this.cbWithNoClick.AutoCheck = false;
            this.cbWithClick.CheckState = CheckState.Checked;
            this.cbWithClick.ThreeState = true;
            this.cbWithClick.AutoCheck = false;
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.Checked)
            {
                ProfileSingleton.GetCurrent().AHK.ahkMode = rb.Name;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
                DisableControlsIfSpeedBoost();
            }
        }

        private void chkMouseFlick_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                ProfileSingleton.GetCurrent().AHK.mouseFlick = chk.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
            }
        }

        private void chkNoShift_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                ProfileSingleton.GetCurrent().AHK.noShift = chk.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
            }
        }

        private void DisableControlsIfSpeedBoost()
        {
            bool isSpeedBoost = ProfileSingleton.GetCurrent().AHK.ahkMode == AHK.SPEED_BOOST;
            this.chkMouseFlick.Enabled = !isSpeedBoost;
            this.chkNoShift.Enabled = !isSpeedBoost;
        }
    }
}
