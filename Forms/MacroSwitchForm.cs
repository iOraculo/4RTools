using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class MacroSwitchForm : Form, IObserver
    {
        public static int TOTAL_MACRO_LANES = 5;
        private bool isUpdatingUi;

        public MacroSwitchForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            NormalizeLabelLayout();
            configureMacroLanes();
        }

        private void NormalizeLabelLayout()
        {
            Label[] keyLabels = new Label[] { this.label5, this.label2, this.label6, this.label9, this.label11 };
            Label[] delayLabels = new Label[] { this.label4, this.label1, this.label3, this.label8, this.label10 };

            foreach (Label lbl in keyLabels)
            {
                lbl.AutoSize = false;
                lbl.Location = new Point(2, lbl.Location.Y - 1);
                lbl.Size = new Size(59, 16);
                lbl.TextAlign = ContentAlignment.MiddleRight;
            }

            foreach (Label lbl in delayLabels)
            {
                lbl.AutoSize = false;
                lbl.Location = new Point(2, lbl.Location.Y - 1);
                lbl.Size = new Size(61, 16);
                lbl.TextAlign = ContentAlignment.MiddleRight;
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
                    updateUi();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().MacroSwitch.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().MacroSwitch.Stop();
                    break;
            }
        }

        private void UpdatePanelData(int id)
        {
            this.isUpdatingUi = true;
            try
            {
                Control[] groups = this.Controls.Find("chainGroup" + id, true);
                if (groups.Length == 0)
                {
                    return;
                }

                GroupBox group = (GroupBox)groups[0];
                Macro macroSwitch = ProfileSingleton.GetCurrent().MacroSwitch;
                ChainConfig existing = macroSwitch.chainConfigs.Find(cfg => cfg.id == id);
                ChainConfig chainConfig = existing != null ? new ChainConfig(existing) : new ChainConfig(id);
                FormUtils.ResetForm(group);

                foreach (KeyValuePair<string, MacroKey> entry in chainConfig.macroEntries)
                {
                    if (entry.Value == null)
                    {
                        continue;
                    }

                    Control[] controls = group.Controls.Find(entry.Key, true);
                    if (controls.Length > 0)
                    {
                        ((TextBox)controls[0]).Text = entry.Value.key.ToString();
                    }

                    Control[] d = group.Controls.Find(entry.Key + "delay", true);
                    if (d.Length > 0)
                    {
                        FormUtils.SetNumericValue((NumericUpDown)d[0], entry.Value.delay);
                    }

                    Control[] c = group.Controls.Find(entry.Key + "click", true);
                    if (c.Length > 0)
                    {
                        ((CheckBox)c[0]).Checked = entry.Value.hasClick;
                    }
                }

                Control[] loopControls = group.Controls.Find("loopMac" + id, true);
                if (loopControls.Length > 0)
                {
                    ((CheckBox)loopControls[0]).Checked = chainConfig.infinityLoop;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("MacroSwitchForm", "Failed to update lane " + id, ex);
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private ChainConfig GetOrCreateChainConfig(int chainID)
        {
            Macro macroSwitch = ProfileSingleton.GetCurrent().MacroSwitch;
            ChainConfig chainConfig = macroSwitch.chainConfigs.Find(config => config.id == chainID);
            if (chainConfig == null)
            {
                chainConfig = new ChainConfig(chainID, Key.None);
                macroSwitch.chainConfigs.Add(chainConfig);
            }

            return chainConfig;
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Parent == null)
            {
                return;
            }

            int chainID = short.Parse(textBox.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            GroupBox group = (GroupBox)this.Controls.Find("chainGroup" + chainID, true)[0];
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            int delayVal = 50;
            Control[] delayControls = group.Controls.Find(textBox.Name + "delay", true);
            if (delayControls.Length > 0)
            {
                delayVal = decimal.ToInt32(((NumericUpDown)delayControls[0]).Value);
            }

            bool hasClick = false;
            Control[] clickControls = group.Controls.Find(textBox.Name + "click", true);
            if (clickControls.Length > 0)
            {
                hasClick = ((CheckBox)clickControls[0]).Checked;
            }

            chainConfig.macroEntries[textBox.Name] = new MacroKey(key, delayVal, hasClick);

            bool isFirstInput = Regex.IsMatch(textBox.Name, "^in1mac" + chainID + "$");
            if (isFirstInput)
            {
                chainConfig.trigger = key;
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null || delayInput.Parent == null)
            {
                return;
            }

            int chainID = short.Parse(delayInput.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);

            string cbName = delayInput.Name.Split(new[] { "delay" }, StringSplitOptions.None)[0];
            int newDelay = decimal.ToInt32(delayInput.Value);
            MacroKey existing;
            if (chainConfig.macroEntries.TryGetValue(cbName, out existing) && existing != null)
            {
                existing.delay = newDelay;
            }
            else
            {
                chainConfig.macroEntries[cbName] = new MacroKey(Key.None, newDelay);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onCheckClickChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox checkInput = sender as CheckBox;
            if (checkInput == null || checkInput.Parent == null)
            {
                return;
            }

            int chainID = short.Parse(checkInput.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);

            string cbName = checkInput.Name.Split(new[] { "click" }, StringSplitOptions.None)[0];
            MacroKey existing;
            if (chainConfig.macroEntries.TryGetValue(cbName, out existing) && existing != null)
            {
                existing.hasClick = checkInput.Checked;
            }
            else
            {
                chainConfig.macroEntries[cbName] = new MacroKey(Key.None, 50, checkInput.Checked);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onLoopCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox loopCheck = sender as CheckBox;
            if (loopCheck == null || !(loopCheck.Tag is int))
            {
                return;
            }

            int chainID = (int)loopCheck.Tag;
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);
            chainConfig.infinityLoop = loopCheck.Checked;
            if (!loopCheck.Checked)
            {
                chainConfig.infinityLoopOn = false;
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void updateUi()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES; i++)
            {
                UpdatePanelData(i);
            }
        }

        private void configureMacroLanes()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES; i++)
            {
                initializeLane(i);
            }
        }

        private void initializeLane(int id)
        {
            try
            {
                Control[] groups = this.Controls.Find("chainGroup" + id, true);
                if (groups.Length == 0)
                {
                    return;
                }

                GroupBox p = (GroupBox)groups[0];
                GetOrCreateChainConfig(id);

                foreach (Control control in p.Controls)
                {
                    if (control is TextBox)
                    {
                        TextBox textBox = (TextBox)control;
                        textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                        textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                        textBox.TextChanged += new EventHandler(this.onTextChange);
                    }
                    else if (control is NumericUpDown)
                    {
                        NumericUpDown delayInput = (NumericUpDown)control;
                        delayInput.ValueChanged += new EventHandler(this.onDelayChange);
                    }
                    else if (control is CheckBox)
                    {
                        CheckBox checkInput = (CheckBox)control;
                        checkInput.CheckedChanged += new EventHandler(this.onCheckClickChange);
                    }
                }

                // Add per-lane toggle loop checkbox neatly below the Delay label.
                CheckBox loopCheck = new CheckBox
                {
                    AutoSize = true,
                    Location = new Point(8, 76),
                    Name = "loopMac" + id,
                    Text = "Loop",
                    Tag = id,
                    UseVisualStyleBackColor = true
                };
                loopCheck.CheckedChanged += new EventHandler(this.onLoopCheckedChanged);
                p.Controls.Add(loopCheck);
            }
            catch (Exception ex)
            {
                Logger.Warn("MacroSwitchForm", "Failed to initialize lane " + id, ex);
            }
        }
    }
}
