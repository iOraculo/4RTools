using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class SkillTimerForm : Form, IObserver
    {
        private bool isUpdatingUi;
        private TextBox txtSkillTimerKey4;
        private NumericUpDown txtAutoRefreshDelay4;
        private TextBox txtSkillTimerKey5;
        private NumericUpDown txtAutoRefreshDelay5;
        private TextBox txtSkillTimerKey6;
        private NumericUpDown txtAutoRefreshDelay6;
        private Font labelFont;
        private Font inputFont;

        public SkillTimerForm(Subject subject)
        {
            InitializeComponent();

            // Normalize row 1 label/input alignment so translated PT-BR labels ("Atraso"/"Tecla"/"seg") never overlap inputs.
            AlignTimerSlotControls(this.lblAutoRefreshDelay, this.txtAutoRefreshDelay, this.label2, this.lblAutoRefreshKey, this.txtSkillTimerKey);
            AlignTimerSlotControls(this.label1, this.txtAutoRefreshDelay2, this.label4, this.label3, this.txtSkillTimerKey2);
            AlignTimerSlotControls(this.label5, this.txtAutoRefreshDelay3, this.label7, this.label6, this.txtSkillTimerKey3);

            InitializeExtraTimerSlots();
            subject.Attach(this);

            SetupTimerInputs(this.txtSkillTimerKey, this.txtAutoRefreshDelay, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer1);
            SetupTimerInputs(this.txtSkillTimerKey2, this.txtAutoRefreshDelay2, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer2);
            SetupTimerInputs(this.txtSkillTimerKey3, this.txtAutoRefreshDelay3, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer3);
            SetupTimerInputs(this.txtSkillTimerKey4, this.txtAutoRefreshDelay4, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer4);
            SetupTimerInputs(this.txtSkillTimerKey5, this.txtAutoRefreshDelay5, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer5);
            SetupTimerInputs(this.txtSkillTimerKey6, this.txtAutoRefreshDelay6, () => ProfileSingleton.GetCurrent().AutoRefreshSpammer6);
        }

        private void InitializeExtraTimerSlots()
        {
            this.labelFont = new Font("Microsoft Sans Serif", 8.5F);
            this.inputFont = new Font("Microsoft Sans Serif", 10F);

            CreateSkillTimerGroup(4, new Point(15, 124), out this.txtSkillTimerKey4, out this.txtAutoRefreshDelay4);
            CreateSkillTimerGroup(5, new Point(194, 124), out this.txtSkillTimerKey5, out this.txtAutoRefreshDelay5);
            CreateSkillTimerGroup(6, new Point(373, 124), out this.txtSkillTimerKey6, out this.txtAutoRefreshDelay6);
        }

        private static void AlignTimerSlotControls(Label lblDelay, NumericUpDown delayBox, Label lblSec, Label lblKey, TextBox keyBox)
        {
            lblDelay.AutoSize = false;
            lblDelay.Location = new Point(4, 27);
            lblDelay.Size = new Size(54, 19);
            lblDelay.TextAlign = ContentAlignment.MiddleRight;

            delayBox.Location = new Point(62, 25);
            delayBox.Size = new Size(58, 23);

            lblSec.Location = new Point(124, 29);

            lblKey.AutoSize = false;
            lblKey.Location = new Point(4, 55);
            lblKey.Size = new Size(54, 19);
            lblKey.TextAlign = ContentAlignment.MiddleRight;

            keyBox.Location = new Point(62, 53);
            keyBox.Size = new Size(58, 23);
        }

        private void CreateSkillTimerGroup(int slotNumber, Point location, out TextBox keyBox, out NumericUpDown delayBox)
        {
            GroupBox group = new GroupBox
            {
                Location = location,
                Name = "groupBox" + slotNumber,
                Size = new Size(173, 100),
                TabStop = false,
                Text = "Skill timer " + slotNumber
            };

            Label lblDelay = new Label
            {
                AutoSize = false,
                Font = this.labelFont,
                Location = new Point(4, 27),
                Name = "lblAutoRefreshDelay" + slotNumber,
                Size = new Size(54, 19),
                Text = "Delay",
                TextAlign = ContentAlignment.MiddleRight
            };

            delayBox = new NumericUpDown
            {
                Font = this.inputFont,
                Location = new Point(62, 25),
                Maximum = new decimal(new int[] { 10000, 0, 0, 0 }),
                Name = "txtAutoRefreshDelay" + slotNumber,
                Size = new Size(58, 23)
            };

            Label lblSec = new Label
            {
                AutoSize = true,
                Font = this.labelFont,
                Location = new Point(124, 29),
                Name = "lblAutoRefreshSec" + slotNumber,
                Text = "sec"
            };

            Label lblKey = new Label
            {
                AutoSize = false,
                Font = this.labelFont,
                Location = new Point(4, 55),
                Name = "lblAutoRefreshKey" + slotNumber,
                Size = new Size(54, 19),
                Text = "Key",
                TextAlign = ContentAlignment.MiddleRight
            };

            keyBox = new TextBox
            {
                Font = this.inputFont,
                Location = new Point(62, 53),
                Name = "txtSkillTimerKey" + slotNumber,
                Size = new Size(58, 23)
            };

            group.Controls.Add(lblDelay);
            group.Controls.Add(delayBox);
            group.Controls.Add(lblKey);
            group.Controls.Add(lblSec);
            group.Controls.Add(keyBox);

            this.Controls.Add(group);
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

                        ApplySpammerToInputs(profile.AutoRefreshSpammer1, this.txtSkillTimerKey, this.txtAutoRefreshDelay);
                        ApplySpammerToInputs(profile.AutoRefreshSpammer2, this.txtSkillTimerKey2, this.txtAutoRefreshDelay2);
                        ApplySpammerToInputs(profile.AutoRefreshSpammer3, this.txtSkillTimerKey3, this.txtAutoRefreshDelay3);
                        ApplySpammerToInputs(profile.AutoRefreshSpammer4, this.txtSkillTimerKey4, this.txtAutoRefreshDelay4);
                        ApplySpammerToInputs(profile.AutoRefreshSpammer5, this.txtSkillTimerKey5, this.txtAutoRefreshDelay5);
                        ApplySpammerToInputs(profile.AutoRefreshSpammer6, this.txtSkillTimerKey6, this.txtAutoRefreshDelay6);
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;
                case MessageCode.TURN_ON:
                    Profile currentOn = ProfileSingleton.GetCurrent();
                    currentOn.AutoRefreshSpammer1.Start();
                    currentOn.AutoRefreshSpammer2.Start();
                    currentOn.AutoRefreshSpammer3.Start();
                    currentOn.AutoRefreshSpammer4.Start();
                    currentOn.AutoRefreshSpammer5.Start();
                    currentOn.AutoRefreshSpammer6.Start();
                    break;
                case MessageCode.TURN_OFF:
                    Profile currentOff = ProfileSingleton.GetCurrent();
                    currentOff.AutoRefreshSpammer1.Stop();
                    currentOff.AutoRefreshSpammer2.Stop();
                    currentOff.AutoRefreshSpammer3.Stop();
                    currentOff.AutoRefreshSpammer4.Stop();
                    currentOff.AutoRefreshSpammer5.Stop();
                    currentOff.AutoRefreshSpammer6.Stop();
                    break;
            }
        }

        private static void ApplySpammerToInputs(AutoRefreshSpammer spammer, TextBox keyBox, NumericUpDown delayBox)
        {
            if (spammer == null || keyBox == null || delayBox == null)
            {
                return;
            }

            keyBox.Text = spammer.RefreshKey.ToString();
            FormUtils.SetNumericValue(delayBox, spammer.RefreshDelay);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.labelFont != null)
            {
                this.labelFont.Dispose();
                this.labelFont = null;
            }
            if (this.inputFont != null)
            {
                this.inputFont.Dispose();
                this.inputFont = null;
            }
            base.OnFormClosed(e);
        }
    }
}
