using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class DebuffRecoveryForm : Form, IObserver
    {
        private sealed class DebuffRowUi
        {
            public Buff Debuff;
            public PictureBox IconBox;
            public Label NameLabel;
            public TextBox KeyBox;
        }

        private readonly List<DebuffRowUi> debuffRows = new List<DebuffRowUi>();
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

            BuildLabeledDebuffGrid();
            subject.Attach(this);
        }

        private void BuildLabeledDebuffGrid()
        {
            this.StatusRecoveryGP.Location = new Point(10, 8);
            this.StatusRecoveryGP.Size = new Size(268, 112);

            this.groupBox1.Location = new Point(288, 8);
            this.groupBox1.Size = new Size(270, 112);

            this.DebuffRecoveryGP.Controls.Clear();
            this.DebuffRecoveryGP.AutoSize = false;
            this.DebuffRecoveryGP.Location = new Point(10, 126);
            this.DebuffRecoveryGP.Size = new Size(548, 144);

            List<Buff> debuffs = Buff.GetDebuffs();
            int rowsPerCol = (debuffs.Count + 1) / 2;

            Panel midDivider = new Panel
            {
                Location = new Point(272, 20),
                Size = new Size(1, Math.Max(60, rowsPerCol * 29 - 4))
            };
            this.DebuffRecoveryGP.Controls.Add(midDivider);

            for (int i = 0; i < debuffs.Count; i++)
            {
                Buff debuff = debuffs[i];
                int col = i / rowsPerCol;
                int row = i % rowsPerCol;
                int baseX = col == 0 ? 12 : 284;
                int y = 20 + (row * 29);

                PictureBox pb = new PictureBox
                {
                    BackColor = Color.Transparent,
                    Image = ThemeManager.StripLightBackground(debuff.icon),
                    Location = new Point(baseX, y),
                    Name = "pbox" + (int)debuff.effectStatusID,
                    Size = new Size(24, 24),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    TabStop = false
                };

                Label lblName = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Location = new Point(baseX + 30, y + 2),
                    Name = "lblDebuff" + (int)debuff.effectStatusID,
                    Size = new Size(152, 20),
                    Text = LocalizationManager.GetBuffShortDisplayName(debuff.name),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                TextBox tb = new TextBox
                {
                    Location = new Point(baseX + 186, y + 2),
                    Name = "in" + (int)debuff.effectStatusID,
                    Size = new Size(58, 20),
                    Tag = (int)debuff.effectStatusID,
                    Text = Key.None.ToString()
                };
                tb.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                tb.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                tb.TextChanged += new EventHandler(onDebuffKeyChange);

                this.DebuffRecoveryGP.Controls.Add(pb);
                this.DebuffRecoveryGP.Controls.Add(lblName);
                this.DebuffRecoveryGP.Controls.Add(tb);

                this.debuffRows.Add(new DebuffRowUi
                {
                    Debuff = debuff,
                    IconBox = pb,
                    NameLabel = lblName,
                    KeyBox = tb
                });
            }

            RefreshLocalizedDebuffLabels();
        }

        private void RefreshLocalizedDebuffLabels()
        {
            foreach (DebuffRowUi row in this.debuffRows)
            {
                if (row == null || row.Debuff == null)
                {
                    continue;
                }

                string shortName = LocalizationManager.GetBuffShortDisplayName(row.Debuff.name);
                string fullTip = LocalizationManager.GetBuffDisplayName(row.Debuff.name);

                row.NameLabel.Text = shortName;
                this.toolTip1.SetToolTip(row.IconBox, fullTip);
                this.toolTip1.SetToolTip(row.NameLabel, fullTip);
                this.toolTip1.SetToolTip(row.KeyBox, fullTip);
            }
        }

        private void onDebuffKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox tb = sender as TextBox;
            if (tb == null || tb.Tag == null)
            {
                return;
            }

            Key k;
            if (!KeyHelper.TryParseKey(tb.Text, out k))
            {
                return;
            }

            EffectStatusIDs statusId = (EffectStatusIDs)(int)tb.Tag;
            DebuffsRecovery debuffsRecovery = ProfileSingleton.GetCurrent().DebuffsRecovery;
            debuffsRecovery.AddKeyToBuff(statusId, k);
            ProfileSingleton.SetConfiguration(debuffsRecovery);
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
                        Dictionary<EffectStatusIDs, Key> debuffMap = ProfileSingleton.GetCurrent().DebuffsRecovery.GetBuffMappingSnapshot();
                        foreach (DebuffRowUi row in this.debuffRows)
                        {
                            Key mapped;
                            row.KeyBox.Text = debuffMap.TryGetValue(row.Debuff.effectStatusID, out mapped)
                                ? mapped.ToString()
                                : Key.None.ToString();
                        }

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
                    RefreshLocalizedDebuffLabels();
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
