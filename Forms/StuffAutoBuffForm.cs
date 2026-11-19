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
        private NumericUpDown numMinSp;
        private NumericUpDown numCooldown;
        private bool isUpdatingOptions;

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
                Location = new Point(46, 10),
                Size = new Size(162, 20),
                Name = "txtStuffSearch",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtSearch.TextChanged += (s, e) => this.renderer.ApplyFilter(this.txtSearch.Text);

            Label lblMinSp = new Label
            {
                AutoSize = true,
                Location = new Point(214, 14),
                Text = "Min SP%:"
            };

            this.numMinSp = new NumericUpDown
            {
                Location = new Point(268, 10),
                Size = new Size(44, 20),
                Minimum = 0,
                Maximum = 99,
                Name = "numStuffMinSp",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numMinSp.ValueChanged += OnAutobuffOptionsChanged;

            Label lblCooldown = new Label
            {
                AutoSize = true,
                Location = new Point(318, 14),
                Text = "CD(ms):"
            };

            this.numCooldown = new NumericUpDown
            {
                Location = new Point(365, 10),
                Size = new Size(64, 20),
                Minimum = 0,
                Maximum = 30000,
                Increment = 250,
                Name = "numStuffCooldown",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numCooldown.ValueChanged += OnAutobuffOptionsChanged;

            Button btnClear = new Button
            {
                Location = new Point(436, 9),
                Size = new Size(94, 22),
                Text = "Clear Stuffs",
                UseVisualStyleBackColor = true
            };
            btnClear.Click += (s, e) =>
            {
                if (MessageBox.Show(
                        LocalizationManager.T("msg.clear_stuffs_confirm"),
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