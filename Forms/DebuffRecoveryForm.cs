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
        private GroupBox gbSpecialDebuffs;
        private GroupBox gbControlDebuffs;
        private Label lblCurableHint;
        private bool isUpdatingUi;

        public DebuffRecoveryForm(Subject subject)
        {
            this.KeyPreview = true;
            InitializeComponent();

            this.txtStatusKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusKey.TextChanged += new EventHandler(onStatusKeyChange);

            // Wire Auto Stand control (Elvira Candy is not present on AureumRO and is omitted from the UI).
            this.autoStandCB.CheckedChanged += new EventHandler(autoStandCB_CheckedChanged);

            BuildCategorizedDebuffLayout();
            subject.Attach(this);
        }

        private void BuildCategorizedDebuffLayout()
        {
            // Remove Elvira Candy card (does not exist on AureumRO).
            if (this.groupBox1 != null)
            {
                this.Controls.Remove(this.groupBox1);
                this.groupBox1.Visible = false;
            }

            // 1. Compact top bar: Posture & Utility (Auto Stand only).
            this.StatusRecoveryGP.Controls.Clear();
            this.StatusRecoveryGP.AutoSize = false;
            this.StatusRecoveryGP.Location = new Point(10, 6);
            this.StatusRecoveryGP.Size = new Size(548, 44);
            this.StatusRecoveryGP.Text = "Posture & Utility";

            this.sitPB.Location = new Point(14, 15);
            this.sitPB.Size = new Size(22, 22);
            this.sitPB.SizeMode = PictureBoxSizeMode.Zoom;

            this.autoStandCB.AutoSize = true;
            this.autoStandCB.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.autoStandCB.Location = new Point(42, 17);
            this.autoStandCB.Text = "Enable Auto Stand (when forced to sit)";

            this.StatusRecoveryGP.Controls.Add(this.sitPB);
            this.StatusRecoveryGP.Controls.Add(this.autoStandCB);

            // 2. Card 1: Panacea / Royal Jelly curable statuses (with 1-key shortcut + individual slots).
            List<Buff> panaceaDebuffs = Buff.GetPanaceaCurableDebuffs();
            int rowStep = 28;
            int panaceaRows = (panaceaDebuffs.Count + 1) / 2;
            int panaceaHeaderHeight = 46;
            int panaceaHeight = panaceaHeaderHeight + 14 + (panaceaRows * rowStep);

            this.DebuffRecoveryGP.Controls.Clear();
            this.DebuffRecoveryGP.AutoSize = false;
            this.DebuffRecoveryGP.Location = new Point(10, 56);
            this.DebuffRecoveryGP.Size = new Size(548, panaceaHeight);
            this.DebuffRecoveryGP.Text = "Panacea / Royal Jelly (Consumable Cure)";

            this.pictureBox1.Location = new Point(12, 18);
            this.pictureBox1.Size = new Size(22, 22);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            this.label1.AutoSize = true;
            this.label1.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            this.label1.Location = new Point(38, 21);
            this.label1.Text = "Single key (all 6):";

            this.txtStatusKey.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            this.txtStatusKey.Location = new Point(198, 19);
            this.txtStatusKey.Size = new Size(58, 20);
            this.txtStatusKey.Text = Key.None.ToString();

            this.lblCurableHint = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                Location = new Point(266, 21),
                Name = "lblCurableHint",
                Size = new Size(270, 18),
                Tag = ThemeManager.TAG_MUTED_TEXT,
                Text = "Or set by status (Green Pot / Holy Water)",
                TextAlign = ContentAlignment.MiddleLeft
            };

            Panel headerDivider = new Panel
            {
                Location = new Point(12, 46),
                Size = new Size(524, 1)
            };

            this.DebuffRecoveryGP.Controls.Add(this.pictureBox1);
            this.DebuffRecoveryGP.Controls.Add(this.label1);
            this.DebuffRecoveryGP.Controls.Add(this.txtStatusKey);
            this.DebuffRecoveryGP.Controls.Add(this.lblCurableHint);
            this.DebuffRecoveryGP.Controls.Add(headerDivider);

            PopulateDebuffGrid(this.DebuffRecoveryGP, panaceaDebuffs, 54, rowStep);

            // 3. Card 2: Special Effects & AureumRO Souls (not cured by Panacea/Royal Jelly).
            List<Buff> specialDebuffs = Buff.GetSpecialAndSoulDebuffs();
            int specialRows = (specialDebuffs.Count + 1) / 2;
            int specialHeight = 28 + (specialRows * rowStep);

            this.gbSpecialDebuffs = new GroupBox
            {
                AutoSize = false,
                Location = new Point(10, this.DebuffRecoveryGP.Bottom + 8),
                Name = "gbSpecialDebuffs",
                Size = new Size(548, specialHeight),
                TabStop = false,
                Text = "Special Effects & Souls (Not cured by Panacea)"
            };
            PopulateDebuffGrid(this.gbSpecialDebuffs, specialDebuffs, 20, rowStep);
            this.Controls.Add(this.gbSpecialDebuffs);

            // 4. Card 3: Crowd Control & Immobilization (Gear Swap / Emergency).
            List<Buff> controlDebuffs = Buff.GetCrowdControlDebuffs();
            int controlRows = (controlDebuffs.Count + 1) / 2;
            int controlHeight = 28 + (controlRows * rowStep);

            this.gbControlDebuffs = new GroupBox
            {
                AutoSize = false,
                Location = new Point(10, this.gbSpecialDebuffs.Bottom + 8),
                Name = "gbControlDebuffs",
                Size = new Size(548, controlHeight),
                TabStop = false,
                Text = "Crowd Control & Immobilization (Gear Swap / Emergency)"
            };
            PopulateDebuffGrid(this.gbControlDebuffs, controlDebuffs, 20, rowStep);
            this.Controls.Add(this.gbControlDebuffs);

            this.AutoScrollMinSize = new Size(0, this.gbControlDebuffs.Bottom + 12);
            RefreshLocalizedDebuffLabels();
        }

        private void PopulateDebuffGrid(GroupBox container, List<Buff> debuffs, int startY, int rowStep)
        {
            int rowsPerCol = (debuffs.Count + 1) / 2;

            Panel midDivider = new Panel
            {
                Location = new Point(272, startY),
                Size = new Size(1, Math.Max(48, rowsPerCol * rowStep - 4))
            };
            container.Controls.Add(midDivider);

            for (int i = 0; i < debuffs.Count; i++)
            {
                Buff debuff = debuffs[i];
                int col = i / rowsPerCol;
                int row = i % rowsPerCol;
                int baseX = col == 0 ? 12 : 284;
                int y = startY + (row * rowStep);

                PictureBox pb = new PictureBox
                {
                    BackColor = Color.Transparent,
                    Image = ThemeManager.StripLightBackground(debuff.icon),
                    Location = new Point(baseX, y),
                    Name = "pbox" + (int)debuff.effectStatusID,
                    Size = new Size(24, 24),
                    SizeMode = PictureBoxSizeMode.Zoom,
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

                container.Controls.Add(pb);
                container.Controls.Add(lblName);
                container.Controls.Add(tb);

                this.debuffRows.Add(new DebuffRowUi
                {
                    Debuff = debuff,
                    IconBox = pb,
                    NameLabel = lblName,
                    KeyBox = tb
                });
            }
        }

        private static string GetDebuffCureHint(EffectStatusIDs statusId)
        {
            bool pt = LocalizationManager.IsPortuguese;
            switch (statusId)
            {
                case EffectStatusIDs.POISON:
                    return pt ? "Curado por: Panaceia, Geleia Real, Poção Verde ou Erva Verde." : "Cured by: Panacea, Royal Jelly, Green Potion, or Green Herb.";
                case EffectStatusIDs.SILENCE:
                case EffectStatusIDs.BLIND:
                    return pt ? "Curado por: Panaceia, Geleia Real ou Poção Verde." : "Cured by: Panacea, Royal Jelly, or Green Potion.";
                case EffectStatusIDs.CURSE:
                    return pt ? "Curado por: Panaceia, Geleia Real, Água Benta ou Bênção." : "Cured by: Panacea, Royal Jelly, Holy Water, or Blessing.";
                case EffectStatusIDs.CONFUSION:
                case EffectStatusIDs.HALLUCINATION:
                    return pt ? "Curado por: Panaceia ou Geleia Real." : "Cured by: Panacea or Royal Jelly.";
                case EffectStatusIDs.EFST_DECREASE_AGI:
                    return pt ? "Removido por: Aumentar Agilidade, Poção do Vento/Guyak ou Bala de Guaraná." : "Countered by: Increase Agility, Speed Potion/Guyak, or Guarana Candy.";
                case EffectStatusIDs.EFST_FREEZING:
                    return pt ? "Alma de Iceicle — Não sai com Panaceia; use dano/skill de Fogo, Desencantar ou troca." : "Iceicle Soul — Not cured by Panacea; use Fire skill, Dispell, or gear swap.";
                case EffectStatusIDs.EFST_BURNING:
                    return pt ? "Almas de Imp e Salamander — Não sai com Panaceia; use Água, cura ou troca." : "Imp & Salamander Souls — Not cured by Panacea; use Water, healing, or gear swap.";
                case EffectStatusIDs.EFST_BLEEDING:
                    return pt ? "Não sai com Panaceia/Geleia — use Cura Livre, Desencantar ou poção de HP." : "Not cured by Panacea/Royal Jelly — use Cure Free, Dispell, or HP pots.";
                case EffectStatusIDs.CRITICALWOUND:
                    return pt ? "Reduz cura recebida — não sai com Panaceia; use Yggdrasil ou troca rápida." : "Reduces healing — not cured by Panacea; use Yggdrasil or quick swap.";
                case EffectStatusIDs.EFST_STONE:
                    return pt ? "Almas de Medusa e Phreeoni — detecta fase inicial (Bênção/Recuperação) e petrificado." : "Medusa & Phreeoni Souls — detects both stone-ing (Blessing/Recovery) and petrified.";
                case EffectStatusIDs.EFST_FREEZE:
                    return pt ? "Almas de Ktullanux, Violy e Swordfish — use para troca rápida de DEF/imunidade." : "Ktullanux, Violy & Swordfish Souls — use for quick DEF/immunity swap.";
                case EffectStatusIDs.EFST_SLEEP:
                    return pt ? "Alma de Nightmare — use para troca rápida de DEF/imunidade ou emergência." : "Nightmare Soul — use for quick DEF/immunity swap or emergency key.";
                case EffectStatusIDs.EFST_SPIDERWEB:
                    return pt ? "Alma de Giant Spider — quebre com ataque/skill de Fogo ou troca rápida." : "Giant Spider Soul — break with Fire attack/skill or quick swap.";
                case EffectStatusIDs.EFST_CURSEDCIRCLE_TARGET:
                    return pt ? "Alma de Chen Verdadeiro — use para troca rápida de DEF ou emergência." : "True Chen Soul — use for quick DEF swap or emergency action.";
                default:
                    return pt ? "Use para troca rápida de DEF/imunidade ou ação de emergência." : "Use for quick DEF/immunity swap or emergency action.";
            }
        }

        private void RefreshLocalizedDebuffLabels()
        {
            string panaceaTip = LocalizationManager.IsPortuguese
                ? "Define uma única tecla (Panaceia ou Geleia Real) para curar Veneno, Silêncio, Cegueira, Maldição, Confusão e Alucinação automaticamente."
                : "Sets one key (Panacea or Royal Jelly) to automatically cure Poison, Silence, Blind, Curse, Confusion, and Hallucination.";
            this.toolTip1.SetToolTip(this.pictureBox1, panaceaTip);
            this.toolTip1.SetToolTip(this.label1, panaceaTip);
            this.toolTip1.SetToolTip(this.txtStatusKey, panaceaTip);

            foreach (DebuffRowUi row in this.debuffRows)
            {
                if (row == null || row.Debuff == null)
                {
                    continue;
                }

                string shortName = LocalizationManager.GetBuffShortDisplayName(row.Debuff.name);
                string fullTip = LocalizationManager.GetBuffDisplayName(row.Debuff.name)
                    + Environment.NewLine
                    + GetDebuffCureHint(row.Debuff.effectStatusID);

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

                        this.autoStandCB.Checked = statusRec.autoStand;
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
    }
}
