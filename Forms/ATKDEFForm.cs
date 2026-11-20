using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ATKDEFForm : Form, IObserver
    {
        private readonly List<Tuple<Control, Control, string>> slotTooltipTargets = new List<Tuple<Control, Control, string>>();
        private bool isUpdatingUi;

        public ATKDEFForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            NormalizeLabelLayout();
            ConfigureAllTenEquipmentSlots();
            SetupInputs();
            RefreshSlotTooltips();
        }

        private void NormalizeLabelLayout()
        {
            this.panelSwitch.Location = new Point(4, 2);
            this.panelSwitch.Size = new Size(576, 240);

            this.groupBoxATKxDEFConfig.Location = new Point(8, 24);
            this.groupBoxATKxDEFConfig.Size = new Size(176, 154);

            this.inSpammerKey.Location = new Point(104, 24);
            this.inSpammerKey.Size = new Size(60, 20);
            this.spammerDelay.Location = new Point(104, 56);
            this.spammerDelay.Size = new Size(60, 20);
            this.switchDelay.Location = new Point(104, 88);
            this.switchDelay.Size = new Size(60, 20);
            this.inSpammerClick.Location = new Point(14, 122);

            Label[] leftLabels = new Label[] { this.label2, this.label3, this.lblSwitchDelay };
            int[] yPositions = new int[] { 26, 58, 90 };
            for (int i = 0; i < leftLabels.Length; i++)
            {
                leftLabels[i].AutoSize = false;
                leftLabels[i].Location = new Point(4, yPositions[i]);
                leftLabels[i].Size = new Size(96, 17);
                leftLabels[i].TextAlign = ContentAlignment.MiddleRight;
            }
        }

        private void ConfigureAllTenEquipmentSlots()
        {
            this.lblDef.Location = new Point(196, 8);
            this.panel1.Location = new Point(376, 10);
            this.panel1.Size = new Size(1, 186);
            this.label1.Location = new Point(388, 8);

            // Create the 5 newly added equipment slots for DEF (Tag = "1") and ATK (Tag = "0"):
            // Hat Top (6), Hat Mid (7), Hat Low (8), Left Accessory (9), Right Accessory (10).
            TextBox inDefTop = CreateEquipInput("inDef6", "1");
            TextBox inDefMid = CreateEquipInput("inDef7", "1");
            TextBox inDefLow = CreateEquipInput("inDef8", "1");
            TextBox inDefAccLeft = CreateEquipInput("inDef9", "1");
            TextBox inDefAccRight = CreateEquipInput("inDef10", "1");

            PictureBox pbDefTop = CreateEquipIcon("pbDefTop", Resources._4RTools.Icons.equip_hat_top);
            PictureBox pbDefMid = CreateEquipIcon("pbDefMid", Resources._4RTools.Icons.equip_hat_mid);
            PictureBox pbDefLow = CreateEquipIcon("pbDefLow", Resources._4RTools.Icons.equip_hat_low);
            PictureBox pbDefAccLeft = CreateEquipIcon("pbDefAccLeft", Resources._4RTools.Icons.equip_acc_left);
            PictureBox pbDefAccRight = CreateEquipIcon("pbDefAccRight", Resources._4RTools.Icons.equip_acc_right);

            TextBox inAtkTop = CreateEquipInput("inAtk6", "0");
            TextBox inAtkMid = CreateEquipInput("inAtk7", "0");
            TextBox inAtkLow = CreateEquipInput("inAtk8", "0");
            TextBox inAtkAccLeft = CreateEquipInput("inAtk9", "0");
            TextBox inAtkAccRight = CreateEquipInput("inAtk10", "0");

            PictureBox pbAtkTop = CreateEquipIcon("pbAtkTop", Resources._4RTools.Icons.equip_hat_top);
            PictureBox pbAtkMid = CreateEquipIcon("pbAtkMid", Resources._4RTools.Icons.equip_hat_mid);
            PictureBox pbAtkLow = CreateEquipIcon("pbAtkLow", Resources._4RTools.Icons.equip_hat_low);
            PictureBox pbAtkAccLeft = CreateEquipIcon("pbAtkAccLeft", Resources._4RTools.Icons.equip_acc_left);
            PictureBox pbAtkAccRight = CreateEquipIcon("pbAtkAccRight", Resources._4RTools.Icons.equip_acc_right);

            int[] rowY = new int[] { 32, 66, 100, 134, 168 };

            // DEF Switch — Column 1 (Top, Mid, Low, Armor, Weapon)
            PlaceEquipSlot(pbDefTop, inDefTop, 196, rowY[0], "equip.hat_top");
            PlaceEquipSlot(pbDefMid, inDefMid, 196, rowY[1], "equip.hat_mid");
            PlaceEquipSlot(pbDefLow, inDefLow, 196, rowY[2], "equip.hat_low");
            PlaceEquipSlot(this.pictureBox1, this.inDef1, 196, rowY[3], "equip.armor");
            PlaceEquipSlot(this.pictureBox2, this.inDef2, 196, rowY[4], "equip.weapon");

            // DEF Switch — Column 2 (Shield, Garment, Shoes, Left Acc, Right Acc)
            PlaceEquipSlot(this.pictureBox5, this.inDef3, 285, rowY[0], "equip.shield");
            PlaceEquipSlot(this.pictureBox3, this.inDef4, 285, rowY[1], "equip.garment");
            PlaceEquipSlot(this.pictureBox4, this.inDef5, 285, rowY[2], "equip.shoes");
            PlaceEquipSlot(pbDefAccLeft, inDefAccLeft, 285, rowY[3], "equip.acc_left");
            PlaceEquipSlot(pbDefAccRight, inDefAccRight, 285, rowY[4], "equip.acc_right");

            // ATK Switch — Column 1 (Top, Mid, Low, Armor, Weapon)
            PlaceEquipSlot(pbAtkTop, inAtkTop, 388, rowY[0], "equip.hat_top");
            PlaceEquipSlot(pbAtkMid, inAtkMid, 388, rowY[1], "equip.hat_mid");
            PlaceEquipSlot(pbAtkLow, inAtkLow, 388, rowY[2], "equip.hat_low");
            PlaceEquipSlot(this.pictureBox10, this.inAtk1, 388, rowY[3], "equip.armor");
            PlaceEquipSlot(this.pictureBox9, this.inAtk2, 388, rowY[4], "equip.weapon");

            // ATK Switch — Column 2 (Shield, Garment, Shoes, Left Acc, Right Acc)
            PlaceEquipSlot(this.pictureBox6, this.inAtk3, 477, rowY[0], "equip.shield");
            PlaceEquipSlot(this.pictureBox8, this.inAtk4, 477, rowY[1], "equip.garment");
            PlaceEquipSlot(this.pictureBox7, this.inAtk5, 477, rowY[2], "equip.shoes");
            PlaceEquipSlot(pbAtkAccLeft, inAtkAccLeft, 477, rowY[3], "equip.acc_left");
            PlaceEquipSlot(pbAtkAccRight, inAtkAccRight, 477, rowY[4], "equip.acc_right");
        }

        private TextBox CreateEquipInput(string name, string modeTag)
        {
            TextBox tb = new TextBox
            {
                Name = name,
                Size = new Size(54, 20),
                Tag = modeTag,
                Text = Key.None.ToString()
            };
            this.panelSwitch.Controls.Add(tb);
            return tb;
        }

        private PictureBox CreateEquipIcon(string name, Image icon)
        {
            PictureBox pb = new PictureBox
            {
                BackColor = Color.Transparent,
                Image = ThemeManager.StripLightBackground(icon),
                Name = name,
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.CenterImage,
                TabStop = false
            };
            this.panelSwitch.Controls.Add(pb);
            return pb;
        }

        private void PlaceEquipSlot(PictureBox pb, TextBox tb, int x, int y, string tooltipKey)
        {
            pb.BackColor = Color.Transparent;
            pb.Size = new Size(24, 24);
            pb.SizeMode = PictureBoxSizeMode.CenterImage;
            pb.Location = new Point(x, y);
            if (pb.Image != null)
            {
                pb.Image = ThemeManager.StripLightBackground(pb.Image);
            }

            tb.Size = new Size(54, 20);
            tb.Location = new Point(x + 27, y + 2);

            this.slotTooltipTargets.Add(Tuple.Create<Control, Control, string>(pb, tb, tooltipKey));
        }

        private void RefreshSlotTooltips()
        {
            if (this.toolTip1 == null)
            {
                return;
            }

            foreach (Tuple<Control, Control, string> entry in this.slotTooltipTargets)
            {
                string label = LocalizationManager.T(entry.Item3);
                this.toolTip1.SetToolTip(entry.Item1, label);
                this.toolTip1.SetToolTip(entry.Item2, label);
            }
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
                    UpdateUI();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshSlotTooltips();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AtkDefMode.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AtkDefMode.Stop();
                    break;
            }
        }

        private void UpdateUI()
        {
            this.isUpdatingUi = true;
            try
            {
                ATKDEFMode atkDef = ProfileSingleton.GetCurrent().AtkDefMode;
                this.inSpammerKey.Text = atkDef.keySpammer.ToString();
                FormUtils.SetNumericValue(this.spammerDelay, atkDef.ahkDelay);
                FormUtils.SetNumericValue(this.switchDelay, atkDef.switchDelay);
                this.inSpammerClick.Checked = atkDef.keySpammerWithClick;

                Dictionary<string, Key> atkKeys = atkDef.GetKeysSnapshot(ATKDEFEnum.ATK);
                Dictionary<string, Key> defKeys = atkDef.GetKeysSnapshot(ATKDEFEnum.DEF);

                foreach (Control control in this.panelSwitch.Controls)
                {
                    TextBox tb = control as TextBox;
                    if (tb == null || tb.Tag == null)
                    {
                        continue;
                    }

                    string tagText = tb.Tag.ToString();
                    if (string.Equals(tagText, "spammerKey", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    short parsedMode;
                    if (!short.TryParse(tagText, out parsedMode))
                    {
                        continue;
                    }

                    ATKDEFEnum mode = (ATKDEFEnum)parsedMode;
                    if (mode == ATKDEFEnum.DEF)
                    {
                        tb.Text = defKeys.ContainsKey(tb.Name) ? defKeys[tb.Name].ToString() : Key.None.ToString();
                    }
                    else
                    {
                        tb.Text = atkKeys.ContainsKey(tb.Name) ? atkKeys[tb.Name].ToString() : Key.None.ToString();
                    }
                }
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null)
            {
                return;
            }

            if (delayInput.Name == "spammerDelay")
            {
                ProfileSingleton.GetCurrent().AtkDefMode.ahkDelay = decimal.ToInt32(delayInput.Value);
            }
            else
            {
                ProfileSingleton.GetCurrent().AtkDefMode.switchDelay = decimal.ToInt32(delayInput.Value);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            string tagText = textBox.Tag.ToString();
            if (string.Equals(tagText, "spammerKey", StringComparison.Ordinal))
            {
                ProfileSingleton.GetCurrent().AtkDefMode.keySpammer = key;
            }
            else
            {
                short parsedMode;
                if (!short.TryParse(tagText, out parsedMode))
                {
                    return;
                }

                ATKDEFEnum mode = (ATKDEFEnum)parsedMode;
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem(textBox.Name, key, mode);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().AtkDefMode.keySpammerWithClick = this.inSpammerClick.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        public void SetupInputs()
        {
            foreach (Control c in FormUtils.GetAll(this, typeof(TextBox)))
            {
                TextBox textBox = c as TextBox;
                if (textBox != null)
                {
                    textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                    textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                    textBox.TextChanged += new EventHandler(this.onTextChange);
                }
            }
        }
    }
}
