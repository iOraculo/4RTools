using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class StuffAutoBuffForm : Form, IObserver
    {
        private readonly List<BuffContainer> stuffContainers = new List<BuffContainer>();
        private readonly BuffRenderer renderer;
        private TextBox txtSearch;

        public StuffAutoBuffForm(Subject subject)
        {
            InitializeComponent();
            this.stuffContainers.Add(new BuffContainer(this.PotionsGP, Buff.GetPotionsBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.ElementalsGP, Buff.GetElementalsBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.BoxesGP, Buff.GetBoxesBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.FoodsGP, Buff.GetFoodBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.ScrollBuffsGP, Buff.GetScrollBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.EtcGP, Buff.GetETCBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.CandiesGP, Buff.GetCandiesBuffs()));
            this.stuffContainers.Add(new BuffContainer(this.ExpGP, Buff.GetEXPBuffs()));

            this.renderer = new BuffRenderer("Autobuff", this.stuffContainers, this.toolTip1);
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
                Name = "txtStuffSearch",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtSearch.TextChanged += (s, e) => this.renderer.ApplyFilter(this.txtSearch.Text);

            Button btnClear = new Button
            {
                Location = new Point(435, 9),
                Size = new Size(95, 22),
                Text = "Clear Stuffs",
                UseVisualStyleBackColor = true
            };
            btnClear.Click += (s, e) =>
            {
                if (MessageBox.Show("Clear all consumable autobuff keys on this tab?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().Autobuff.Stop();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().Autobuff.Start();
                    break;
            }
        }
    }
}