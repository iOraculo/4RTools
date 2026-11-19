using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;

namespace _4RTools.Utils
{
    public class KeybindConflict
    {
        public Key Key { get; private set; }
        public List<string> Usages { get; private set; }

        public KeybindConflict(Key key, List<string> usages)
        {
            this.Key = key;
            this.Usages = usages ?? new List<string>();
        }
    }

    public static class KeybindConflictDetector
    {
        public static List<KeybindConflict> DetectConflicts(Profile profile)
        {
            List<KeybindConflict> conflicts = new List<KeybindConflict>();
            if (profile == null)
            {
                return conflicts;
            }

            Dictionary<Key, List<string>> usagesByKey = new Dictionary<Key, List<string>>();

            if (profile.UserPreferences != null && !string.IsNullOrWhiteSpace(profile.UserPreferences.toggleStateKey))
            {
                Keys toggleFormsKey;
                if (KeyHelper.TryParseFormsKeys(profile.UserPreferences.toggleStateKey, out toggleFormsKey) && toggleFormsKey != Keys.None)
                {
                    AddUsage(usagesByKey, KeyHelper.FromFormsKeys(toggleFormsKey), "ON/OFF Toggle Hotkey");
                }
            }

            if (profile.Autopot != null)
            {
                AddUsage(usagesByKey, profile.Autopot.hpKey, "Autopot (HP)");
                AddUsage(usagesByKey, profile.Autopot.spKey, "Autopot (SP)");
            }

            if (profile.AutopotYgg != null)
            {
                AddUsage(usagesByKey, profile.AutopotYgg.hpKey, "Yggdrasil Autopot (HP)");
                AddUsage(usagesByKey, profile.AutopotYgg.spKey, "Yggdrasil Autopot (SP)");
            }

            if (profile.AHK != null)
            {
                foreach (KeyValuePair<string, KeyConfig> entry in profile.AHK.GetEntriesSnapshot())
                {
                    if (entry.Value != null)
                    {
                        string modeLabel = entry.Value.ClickActive ? "Skill Spammer (Click)" : "Skill Spammer (No-Click)";
                        AddUsage(usagesByKey, entry.Value.key, modeLabel);
                    }
                }
            }

            if (profile.Autobuff != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.Autobuff.GetBuffMappingSnapshot())
                {
                    AddUsage(usagesByKey, entry.Value, "Autobuff (" + entry.Key + ")");
                }
            }

            // Multiple status ailments intentionally share one cure item (e.g. Panacea / Green Potion),
            // so collapse duplicate uses within the same recovery group to avoid false positives.
            if (profile.StatusRecovery != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.StatusRecovery.GetBuffMappingSnapshot())
                {
                    AddUsage(usagesByKey, entry.Value, "Status Recovery");
                }
            }

            if (profile.DebuffsRecovery != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.DebuffsRecovery.GetBuffMappingSnapshot())
                {
                    AddUsage(usagesByKey, entry.Value, "Debuff Recovery");
                }
            }

            if (profile.AutoRefreshSpammer1 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer1.RefreshKey, "Skill Timer 1");
            }

            if (profile.AutoRefreshSpammer2 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer2.RefreshKey, "Skill Timer 2");
            }

            if (profile.AutoRefreshSpammer3 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer3.RefreshKey, "Skill Timer 3");
            }

            if (profile.AtkDefMode != null)
            {
                AddUsage(usagesByKey, profile.AtkDefMode.keySpammer, "ATK/DEF Spammer");
                foreach (KeyValuePair<string, Key> entry in profile.AtkDefMode.GetKeysSnapshot(ATKDEFEnum.ATK))
                {
                    AddUsage(usagesByKey, entry.Value, "ATK/DEF (ATK Equip)");
                }
                foreach (KeyValuePair<string, Key> entry in profile.AtkDefMode.GetKeysSnapshot(ATKDEFEnum.DEF))
                {
                    AddUsage(usagesByKey, entry.Value, "ATK/DEF (DEF Equip)");
                }
            }

            CollectMacroUsages(usagesByKey, profile.SongMacro, "Song Macro");
            CollectMacroUsages(usagesByKey, profile.MacroSwitch, "Macro Switch");

            foreach (KeyValuePair<Key, List<string>> pair in usagesByKey.OrderBy(p => p.Key.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                if (pair.Value.Count > 1)
                {
                    conflicts.Add(new KeybindConflict(pair.Key, pair.Value));
                }
            }

            return conflicts;
        }

        public static string FormatReport(string profileName, List<KeybindConflict> conflicts)
        {
            string safeName = string.IsNullOrWhiteSpace(profileName) ? "Current" : profileName;
            if (conflicts == null || conflicts.Count == 0)
            {
                return "No keybind conflicts detected in profile '" + safeName + "'!";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Found {0} keybind conflict(s) in profile '{1}':", conflicts.Count, safeName);
            sb.AppendLine();
            sb.AppendLine();

            foreach (KeybindConflict conflict in conflicts)
            {
                sb.AppendFormat("- [{0}]: {1}", conflict.Key, string.Join("  |  ", conflict.Usages));
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static void CollectMacroUsages(Dictionary<Key, List<string>> usagesByKey, Macro macro, string macroLabel)
        {
            if (macro == null || macro.chainConfigs == null)
            {
                return;
            }

            foreach (ChainConfig lane in macro.chainConfigs)
            {
                if (lane == null)
                {
                    continue;
                }

                AddUsage(usagesByKey, lane.trigger, macroLabel + " (Lane " + lane.id + " Trigger)");
                AddUsage(usagesByKey, lane.instrumentKey, macroLabel + " (Instrument)");
                AddUsage(usagesByKey, lane.daggerKey, macroLabel + " (Dagger)");

                if (lane.macroEntries != null)
                {
                    foreach (KeyValuePair<string, MacroKey> slot in lane.macroEntries)
                    {
                        if (slot.Value != null)
                        {
                            AddUsage(usagesByKey, slot.Value.key, macroLabel + " (Lane " + lane.id + " Slot)");
                        }
                    }
                }
            }
        }

        private static void AddUsage(Dictionary<Key, List<string>> usagesByKey, Key key, string label)
        {
            if (!FormUtils.IsValidKey(key) || string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            List<string> list;
            if (!usagesByKey.TryGetValue(key, out list))
            {
                list = new List<string>();
                usagesByKey[key] = list;
            }

            if (!list.Contains(label))
            {
                list.Add(label);
            }
        }
    }
}
