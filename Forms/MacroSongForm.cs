using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class MacroSongForm : Form, IObserver
    {
        public static int TOTAL_MACRO_LANES_FOR_SONGS = 4;
        private bool isUpdatingUi;

        public MacroSongForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            NormalizeLabelLayout();
            configureMacroLanes();
        }

        private void NormalizeLabelLayout()
        {
            Label[] keyLabels = new Label[] { this.label6, this.label3, this.label9, this.label12 };
            Label[] delayLabels = new Label[] { this.label5, this.label2, this.label8, this.label11 };
            Label[] msLabels = new Label[] { this.label4, this.label1, this.label7, this.label10 };
            TextBox[] triggerBoxes = new TextBox[] { this.inTriggerMacro1, this.inTriggerMacro2, this.inTriggerMacro3, this.inTriggerMacro4 };
            NumericUpDown[] delayBoxes = new NumericUpDown[] { this.delayMac1, this.delayMac2, this.delayMac3, this.delayMac4 };

            for (int i = 0; i < 4; i++)
            {
                keyLabels[i].AutoSize = false;
                keyLabels[i].Location = new Point(2, keyLabels[i].Location.Y - 1);
                keyLabels[i].Size = new Size(44, 16);
                keyLabels[i].TextAlign = ContentAlignment.MiddleRight;

                delayLabels[i].AutoSize = false;
                delayLabels[i].Location = new Point(2, delayLabels[i].Location.Y - 1);
                delayLabels[i].Size = new Size(44, 16);
                delayLabels[i].TextAlign = ContentAlignment.MiddleRight;

                triggerBoxes[i].Location = new Point(49, triggerBoxes[i].Location.Y);
                delayBoxes[i].Location = new Point(49, delayBoxes[i].Location.Y);
                msLabels[i].Location = new Point(98, msLabels[i].Location.Y);
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
                    ProfileSingleton.GetCurrent().SongMacro.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().SongMacro.Stop();
                    break;
            }
        }

        private void UpdatePanelData(int id)
        {
            this.isUpdatingUi = true;
            try
            {
                Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
                Control[] panels = this.Controls.Find("panelMacro" + id, true);
                if (panels.Length == 0)
                {
                    return;
                }

                Panel p = (Panel)panels[0];
                ChainConfig existing = songMacro.chainConfigs.Find(cfg => cfg.id == id);
                ChainConfig chainConfig = existing != null ? new ChainConfig(existing) : new ChainConfig(id);
                FormUtils.ResetForm(p);

                // Update Trigger Macro Value
                Control[] cTrigger = p.Controls.Find("inTriggerMacro" + chainConfig.id, true);
                if (cTrigger.Length > 0)
                {
                    ((TextBox)cTrigger[0]).Text = chainConfig.trigger.ToString();
                }

                // Update Dagger Value
                Control[] cDagger = p.Controls.Find("inDaggerMacro" + chainConfig.id, true);
                if (cDagger.Length > 0)
                {
                    ((TextBox)cDagger[0]).Text = chainConfig.daggerKey.ToString();
                }

                // Update Instrument Value
                Control[] cInstrument = p.Controls.Find("inInstrumentMacro" + chainConfig.id, true);
                if (cInstrument.Length > 0)
                {
                    ((TextBox)cInstrument[0]).Text = chainConfig.instrumentKey.ToString();
                }

                foreach (KeyValuePair<string, MacroKey> entry in chainConfig.macroEntries)
                {
                    Control[] controls = p.Controls.Find(entry.Key, true);
                    if (controls.Length > 0 && entry.Value != null)
                    {
                        ((TextBox)controls[0]).Text = entry.Value.key.ToString();
                    }
                }

                // Update Delay Macro Value
                Control[] d = p.Controls.Find("delayMac" + chainConfig.id, true);
                if (d.Length > 0)
                {
                    FormUtils.SetNumericValue((NumericUpDown)d[0], chainConfig.delay);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("MacroSongForm", "Failed to update panel " + id, ex);
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            if (textBox.Tag != null)
            {
                string[] inputTag = textBox.Tag.ToString().Split(new[] { ":" }, StringSplitOptions.None);
                if (inputTag.Length < 2)
                {
                    return;
                }

                int macroid = short.Parse(inputTag[0]);
                string type = inputTag[1];
                ChainConfig chainConfig = songMacro.chainConfigs.Find(config => config.id == macroid);
                if (chainConfig == null)
                {
                    return;
                }

                switch (type)
                {
                    case "Dagger":
                        chainConfig.daggerKey = key;
                        break;
                    case "Instrument":
                        chainConfig.instrumentKey = key;
                        break;
                    case "Trigger":
                        chainConfig.trigger = key;
                        break;
                }
            }
            else
            {
                int macroID = short.Parse(textBox.Name.Split(new[] { "mac" }, StringSplitOptions.None)[1]);
                ChainConfig chainConfig = songMacro.chainConfigs.Find(cfg => cfg.id == macroID);
                if (chainConfig == null)
                {
                    return;
                }

                chainConfig.macroEntries[textBox.Name] = new MacroKey(key, chainConfig.delay);
            }

            ProfileSingleton.SetConfiguration(songMacro);
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null)
            {
                return;
            }

            int macroID = short.Parse(delayInput.Name.Split(new[] { "delayMac" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = songMacro.chainConfigs.Find(cfg => cfg.id == macroID);
            if (chainConfig == null)
            {
                return;
            }

            chainConfig.delay = decimal.ToInt32(delayInput.Value);

            foreach (MacroKey entry in chainConfig.macroEntries.Values)
            {
                if (entry != null)
                {
                    entry.delay = chainConfig.delay;
                }
            }

            ProfileSingleton.SetConfiguration(songMacro);
        }

        private void onReset(object sender, EventArgs e)
        {
            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            Button resetButton = sender as Button;
            if (resetButton == null)
            {
                return;
            }

            int btnResetID = short.Parse(resetButton.Name.Split(new[] { "btnResMac" }, StringSplitOptions.None)[1]);
            songMacro.ResetMacro(btnResetID);
            ProfileSingleton.SetConfiguration(songMacro);
            UpdatePanelData(btnResetID);
        }

        private void updateUi()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES_FOR_SONGS; i++)
            {
                UpdatePanelData(i);
            }
        }

        private void configureMacroLanes()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES_FOR_SONGS; i++)
            {
                initializeLane(i);
            }
        }

        private void initializeLane(int id)
        {
            try
            {
                Control[] panels = this.Controls.Find("panelMacro" + id, true);
                if (panels.Length == 0)
                {
                    return;
                }

                Panel p = (Panel)panels[0];
                foreach (Control c in p.Controls)
                {
                    if (c is TextBox)
                    {
                        TextBox textBox = (TextBox)c;
                        textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                        textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                        textBox.TextChanged += new EventHandler(this.onTextChange);
                    }
                    else if (c is Button)
                    {
                        Button resetButton = (Button)c;
                        resetButton.Click += new EventHandler(this.onReset);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("MacroSongForm", "Failed to initialize lane " + id, ex);
            }
        }
    }
}
