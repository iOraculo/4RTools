using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class DebuffRecoveryForm : Form, IObserver
    {
        private readonly List<BuffContainer> skillContainers = new List<BuffContainer>();
        private readonly BuffRenderer renderer;
        private bool isUpdatingUi;

        public DebuffRecoveryForm(Subject subject)
        {
            this.KeyPreview = true;
            InitializeComponent();

            this.txtStatusKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusKey.TextChanged += new EventHandler(onStatusKeyChange);
            this.txtNewStatusKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtNewStatusKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtNewStatusKey.TextChanged += new EventHandler(on3RDStatusKeyChange);

            // Wire controls that were disconnected in Designer.
            this.autoStandCB.CheckedChanged += new EventHandler(autoStandCB_CheckedChanged);
            this.newStatusDelay.Maximum = 60000;
            this.newStatusDelay.ValueChanged += new EventHandler(onNewStatusDelayChanged);

            this.skillContainers.Add(new BuffContainer(this.DebuffRecoveryGP, Buff.GetDebuffs()));
            this.renderer = new BuffRenderer("DebuffsRecovery", this.skillContainers, this.toolTip1);
            this.renderer.doRender();

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
                        BuffRenderer.doUpdate(ProfileSingleton.GetCurrent().DebuffsRecovery.GetBuffMappingSnapshot(), this);

                        StatusRecovery statusRec = ProfileSingleton.GetCurrent().StatusRecovery;
                        Dictionary<EffectStatusIDs, Key> statusMap = statusRec.GetBuffMappingSnapshot();

                        this.txtStatusKey.Text = statusMap.ContainsKey(EffectStatusIDs.SILENCE)
                            ? statusMap[EffectStatusIDs.SILENCE].ToString()
                            : Key.None.ToString();

                        this.txtNewStatusKey.Text = statusMap.ContainsKey(EffectStatusIDs.CRITICALWOUND)
                            ? statusMap[EffectStatusIDs.CRITICALWOUND].ToString()
                            : Key.None.ToString();

                        this.autoStandCB.Checked = statusRec.autoStand;
                        FormUtils.SetNumericValue(this.newStatusDelay, statusRec.newStatusDelay);
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    if (this.renderer != null)
                    {
                        this.renderer.RefreshLocalizedTooltips();
                    }
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().StatusRecovery.Stop();
                    ProfileSingleton.GetCurrent().DebuffsRecovery.Stop();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().StatusRecovery.Start();
                    ProfileSingleton.GetCurrent().DebuffsRecovery.Start();
                    break;
            }
        }

        private void onStatusKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Key k;
            if (!KeyHelper.TryParseKey(this.txtStatusKey.Text, out k))
            {
                return;
            }

            StatusRecovery statusRec = ProfileSingleton.GetCurrent().StatusRecovery;
            statusRec.AddKeyToBuff(EffectStatusIDs.POISON, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.SILENCE, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.BLIND, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.CONFUSION, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.HALLUCINATIONWALK, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.HALLUCINATION, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.CURSE, k);

            ProfileSingleton.SetConfiguration(statusRec);
        }

        private void on3RDStatusKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Key k;
            if (!KeyHelper.TryParseKey(this.txtNewStatusKey.Text, out k))
            {
                return;
            }

            StatusRecovery statusRec = ProfileSingleton.GetCurrent().StatusRecovery;
            statusRec.AddKeyToBuff(EffectStatusIDs.CRITICALWOUND, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.PROPERTYUNDEAD, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.BLOODING, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.MISTY_FROST, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.OVERHEAT, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.EFST_MANDRAGORA, k);

            ProfileSingleton.SetConfiguration(statusRec);
        }

        private void autoStandCB_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                ProfileSingleton.GetCurrent().StatusRecovery.autoStand = chk.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().StatusRecovery);
            }
        }

        private void onNewStatusDelayChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().StatusRecovery.newStatusDelay = decimal.ToInt32(this.newStatusDelay.Value);
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().StatusRecovery);
        }
    }
}