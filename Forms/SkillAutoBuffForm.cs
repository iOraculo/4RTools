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
            subject.Attach(this);
        }

        private void InitializeSearchBar()
        {
            Label lblSearch = new Label
            {
                AutoSize = true,
                Location = new Point(10, 14),
                Text = "Filter:"
            };

            this.txtSearch = new TextBox
            {
                Location = new Point(52, 10),
                Size = new Size(375, 20),
                Name = "txtSkillSearch",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtSearch.TextChanged += (s, e) => this.renderer.ApplyFilter(this.txtSearch.Text);

            Button btnClear = new Button
            {
                Location = new Point(435, 9),
                Size = new Size(95, 22),
                Text = "Clear Skills",
                UseVisualStyleBackColor = true
            };
            btnClear.Click += (s, e) =>
            {
                if (MessageBox.Show("Clear all skill autobuff keys on this tab?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.renderer.ClearAll();
                }
            };

            this.Controls.Add(lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(btnClear);
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
                    break;
            }
        }
    }
}