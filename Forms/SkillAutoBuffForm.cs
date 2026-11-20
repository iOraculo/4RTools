using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class SkillAutoBuffForm : Form, IObserver
    {
        private readonly List<BuffContainer> skillContainers = new List<BuffContainer>();
        private readonly BuffRenderer renderer;
        private TextBox txtSearch;
        private NumericUpDown numMinSp;
        private NumericUpDown numCooldown;
        private bool isUpdatingOptions;

        public SkillAutoBuffForm(Subject subject)
        {
            this.KeyPreview = true;
            InitializeComponent();

            this.skillContainers.Add(new BuffContainer(this.ArcherSkillsGP, Buff.GetArcherSkills()));
            this.skillContainers.Add(new BuffContainer(this.SwordmanSkillGP, Buff.GetSwordmanSkill()));
            this.skillContainers.Add(new BuffContainer(this.MageSkillGP, Buff.GetMageSkills()));
            this.skillContainers.Add(new BuffContainer(this.MerchantSkillsGP, Buff.GetMerchantSkills()));
            this.skillContainers.Add(new BuffContainer(this.ThiefSkillsGP, Buff.GetThiefSkills()));
            this.skillContainers.Add(new BuffContainer(this.AcolyteSkillsGP, Buff.GetAcolyteSkills()));
            this.skillContainers.Add(new BuffContainer(this.TKSkillGroupBox, Buff.GetTaekwonSkills()));
            this.skillContainers.Add(new BuffContainer(this.NinjaSkillsGP, Buff.GetNinjaSkills()));
            this.skillContainers.Add(new BuffContainer(this.GunsSkillsGP, Buff.GetGunsSkills()));
            this.skillContainers.Add(new BuffContainer(this.summonerSkillsGP, Buff.GetSummonerSkills()));
            this.skillContainers.Add(new BuffContainer(this.SoulAsceticSkillsGP, Buff.GetSoulAsceticSkills()));
            this.skillContainers.Add(new BuffContainer(this.NightWatchSkillsGP, Buff.GetNightWatchSkills()));
            this.skillContainers.Add(new BuffContainer(this.HyperNoviceSkillsGP, Buff.GetHyperNoviceSkills()));
            this.skillContainers.Add(new BuffContainer(this.HomunculusSkillsGP, Buff.GetHomunculusBuffs()));

            this.renderer = new BuffRenderer("Autobuff", this.skillContainers, this.toolTip1);
            InitializeSearchBar();
            this.renderer.doRender(42);
            this.VisibleChanged += (s, e) => SyncAutobuffOptionsFromProfile();
            subject.Attach(this);
        }

        private void InitializeSearchBar()
        {
            Label lblSearch = new Label
            {
                AutoSize = true,
                Location = new Point(8, 14),
                Text = "Filter:"
            };

            this.txtSearch = new TextBox
            {
                Location = new Point(54, 10),
                Size = new Size(142, 20),
                Name = "txtSkillSearch",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtSearch.TextChanged += (s, e) => this.renderer.ApplyFilter(this.txtSearch.Text);

            Label lblMinSp = new Label
            {
                AutoSize = true,
                Location = new Point(204, 14),
                Text = "Min SP%:"
            };

            this.numMinSp = new NumericUpDown
            {
                Location = new Point(264, 10),
                Size = new Size(44, 20),
                Minimum = 0,
                Maximum = 99,
                Name = "numSkillMinSp",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numMinSp.ValueChanged += OnAutobuffOptionsChanged;

            Label lblCooldown = new Label
            {
                AutoSize = true,
                Location = new Point(316, 14),
                Text = "CD(ms):"
            };

            this.numCooldown = new NumericUpDown
            {
                Location = new Point(368, 10),
                Size = new Size(62, 20),
                Minimum = 0,
                Maximum = 30000,
                Increment = 250,
                Name = "numSkillCooldown",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numCooldown.ValueChanged += OnAutobuffOptionsChanged;

            Button btnClear = new Button
            {
                Location = new Point(438, 9),
                Size = new Size(104, 22),
                Text = "Clear Skills",
                UseVisualStyleBackColor = true
            };
            btnClear.Click += (s, e) =>
            {
                if (MessageBox.Show(
                        LocalizationManager.T("msg.clear_skills_confirm"),
                        LocalizationManager.T("title.confirm"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.renderer.ClearAll();
                }
            };

            this.Controls.Add(lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(lblMinSp);
            this.Controls.Add(this.numMinSp);
            this.Controls.Add(lblCooldown);
            this.Controls.Add(this.numCooldown);
            this.Controls.Add(btnClear);

            RefreshLocalizedTooltips();
        }

        private void RefreshLocalizedTooltips()
        {
            if (this.numMinSp != null)
            {
                this.toolTip1.SetToolTip(this.numMinSp, LocalizationManager.T("tooltip.min_sp"));
            }
            if (this.numCooldown != null)
            {
                this.toolTip1.SetToolTip(this.numCooldown, LocalizationManager.T("tooltip.buff_cd"));
            }
            if (this.renderer != null)
            {
                this.renderer.RefreshLocalizedTooltips();
                if (this.txtSearch != null && !string.IsNullOrWhiteSpace(this.txtSearch.Text))
                {
                    this.renderer.ApplyFilter(this.txtSearch.Text);
                }
            }
        }

        private void SyncAutobuffOptionsFromProfile()
        {
            if (this.numMinSp == null || this.numCooldown == null)
            {
                return;
            }

            this.isUpdatingOptions = true;
            try
            {
                AutoBuff autobuff = ProfileSingleton.GetCurrent().Autobuff;
                FormUtils.SetNumericValue(this.numMinSp, autobuff.minSpPercent);
                FormUtils.SetNumericValue(this.numCooldown, autobuff.buffCooldownMs);
            }
            finally
            {
                this.isUpdatingOptions = false;
            }
        }

        private void OnAutobuffOptionsChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingOptions || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            AutoBuff autobuff = ProfileSingleton.GetCurrent().Autobuff;
            autobuff.minSpPercent = Convert.ToInt32(this.numMinSp.Value);
            autobuff.buffCooldownMs = Convert.ToInt32(this.numCooldown.Value);
            ProfileSingleton.SetConfiguration(autobuff);
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
                    BuffRenderer.doUpdate(ProfileSingleton.GetCurrent().Autobuff.GetBuffMappingSnapshot(), this);
                    SyncAutobuffOptionsFromProfile();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshLocalizedTooltips();
                    break;
            }
        }
    }
}