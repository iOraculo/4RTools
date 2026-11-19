using System;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class SkillTimerForm : Form, IObserver
    {
        private bool isUpdatingUi;

        public SkillTimerForm(Subject subject)
        {
            InitializeComponent();
            subject.Attach(this);

            SetupTimerInputs(this.txtSkillTimerKey, this.txtAutoRefreshDelay, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer1);
            SetupTimerInputs(this.txtSkillTimerKey2, this.txtAutoRefreshDelay2, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer2);
            SetupTimerInputs(this.txtSkillTimerKey3, this.txtAutoRefreshDelay3, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer3);
        }

        private void SetupTimerInputs(TextBox keyBox, NumericUpDown delayBox, Func<AutoRefreshSpammer> getSpammer)
        {
            keyBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            keyBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            keyBox.TextChanged += (sender, e) => OnTimerKeyChanged(keyBox, getSpammer());
            delayBox.ValueChanged += (sender, e) => OnTimerDelayChanged(delayBox, getSpammer());
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
                        Profile profile = ProfileSingleton.GetCurrent();
                        FormUtils.ResetForm(this);

                        this.txtSkillTimerKey.Text = profile.AutoRefreshSpammer1.RefreshKey.ToString();
                        FormUtils.SetNumericValue(this.txtAutoRefreshDelay, profile.AutoRefreshSpammer1.RefreshDelay);

                        this.txtSkillTimerKey2.Text = profile.AutoRefreshSpammer2.RefreshKey.ToString();
                        FormUtils.SetNumericValue(this.txtAutoRefreshDelay2, profile.AutoRefreshSpammer2.RefreshDelay);

                        this.txtSkillTimerKey3.Text = profile.AutoRefreshSpammer3.RefreshKey.ToString();
                        FormUtils.SetNumericValue(this.txtAutoRefreshDelay3, profile.AutoRefreshSpammer3.RefreshDelay);
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AutoRefreshSpammer1.Start();
                    ProfileSingleton.GetCurrent().AutoRefreshSpammer2.Start();
                    ProfileSingleton.GetCurrent().AutoRefreshSpammer3.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AutoRefreshSpammer1.Stop();
                    ProfileSingleton.GetCurrent().AutoRefreshSpammer2.Stop();
                    ProfileSingleton.GetCurrent().AutoRefreshSpammer3.Stop();
                    break;
            }
        }

        private void OnTimerKeyChanged(TextBox keyBox, AutoRefreshSpammer spammer)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || spammer == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(keyBox.Text, out key))
            {
                spammer.RefreshKey = key;
                ProfileSingleton.SetConfiguration(spammer);
            }
        }

        private void OnTimerDelayChanged(NumericUpDown delayBox, AutoRefreshSpammer spammer)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || spammer == null)
            {
                return;
            }

            spammer.RefreshDelay = decimal.ToInt32(delayBox.Value);
            ProfileSingleton.SetConfiguration(spammer);
        }
    }
}
