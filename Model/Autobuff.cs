using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class AutoBuff : Action
    {
        public static string ACTION_NAME_AUTOBUFF = "Autobuff";

        private static readonly HashSet<EffectStatusIDs> QuagmireBlockedBuffs = new HashSet<EffectStatusIDs>
        {
            EffectStatusIDs.CONCENTRATION,
            EffectStatusIDs.INC_AGI,
            EffectStatusIDs.TRUESIGHT,
            EffectStatusIDs.ADRENALINE,
            EffectStatusIDs.SPEARQUICKEN
        };

        private static readonly HashSet<EffectStatusIDs> IncapacitatingDebuffs = new HashSet<EffectStatusIDs>
        {
            EffectStatusIDs.SILENCE,
            EffectStatusIDs.EFST_FREEZING,
            EffectStatusIDs.EFST_STUN,
            EffectStatusIDs.EFST_DEEP_SLEEP
        };

        private readonly object mappingLock = new object();
        private readonly Dictionary<EffectStatusIDs, long> lastCastTimesMs = new Dictionary<EffectStatusIDs, long>();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private _4RThread thread;

        public int delay { get; set; } = 1;
        public int minSpPercent { get; set; } = 0;
        public int buffCooldownMs { get; set; } = 0;
        public bool pauseOnDebuff { get; set; } = true;
        public Dictionary<EffectStatusIDs, Key> buffMapping = new Dictionary<EffectStatusIDs, Key>();

        public void Start()
        {
            Stop();
            ResetCooldowns();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = AutoBuffThread(roClient);
                _4RThread.Start(this.thread);
            }
        }

        public _4RThread AutoBuffThread(Client c)
        {
            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
            byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];

            return new _4RThread(token =>
            {
                if (c.HasMemorySupport && c.IsProcessAlive() && !c.ShouldPauseAutomation())
                {
                    bool foundQuag = false;
                    bool foundBlockingDebuff = false;
                    Dictionary<EffectStatusIDs, Key> missingBuffs = GetBuffMappingSnapshot();

                    if (missingBuffs.Count > 0)
                    {
                        int count = c.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                        for (int i = 0; i < count; i++)
                        {
                            EffectStatusIDs status = (EffectStatusIDs)statusBuffer[i];

                            if (status == EffectStatusIDs.IGNORE || status == EffectStatusIDs.IGNORE_2)
                            {
                                continue;
                            }

                            if (status == EffectStatusIDs.OVERTHRUSTMAX)
                            {
                                missingBuffs.Remove(EffectStatusIDs.OVERTHRUST);
                            }

                            missingBuffs.Remove(status);

                            if (status == EffectStatusIDs.QUAGMIRE)
                            {
                                foundQuag = true;
                            }
                            else if (IncapacitatingDebuffs.Contains(status))
                            {
                                foundBlockingDebuff = true;
                            }
                        }

                        if (missingBuffs.Count > 0)
                        {
                            uint currentHp;
                            uint maxHp;
                            uint currentSp;
                            uint maxSp;
                            bool hasVitals = c.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp);
                            if (hasVitals && currentHp >= Constants.MINIMUM_HP_TO_RECOVER)
                            {
                                long nowMs = this.clock.ElapsedMilliseconds;
                                foreach (KeyValuePair<EffectStatusIDs, Key> item in missingBuffs)
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        break;
                                    }

                                    if (!ShouldCastBuff(item.Key, nowMs, foundQuag, foundBlockingDebuff, currentSp, maxSp))
                                    {
                                        continue;
                                    }

                                    UseAutobuff(c, item.Value);
                                    RecordBuffCast(item.Key, nowMs);
                                    if (!_4RThread.Sleep(10, token))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                _4RThread.Sleep(300, token);
            }, ACTION_NAME_AUTOBUFF);
        }

        public bool ShouldCastBuff(
            EffectStatusIDs status,
            long nowMs,
            bool hasQuagmire,
            bool hasBlockingDebuff,
            uint currentSp,
            uint maxSp)
        {
            if (this.pauseOnDebuff && hasBlockingDebuff)
            {
                return false;
            }

            if (hasQuagmire && QuagmireBlockedBuffs.Contains(status))
            {
                return false;
            }

            if (this.minSpPercent > 0 && Client.IsBelowPercent(currentSp, maxSp, this.minSpPercent))
            {
                return false;
            }

            int cooldown = this.buffCooldownMs;
            if (cooldown > 0)
            {
                lock (this.mappingLock)
                {
                    long lastCast;
                    if (this.lastCastTimesMs.TryGetValue(status, out lastCast) && (nowMs - lastCast) < cooldown)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void RecordBuffCast(EffectStatusIDs status, long nowMs)
        {
            lock (this.mappingLock)
            {
                this.lastCastTimesMs[status] = nowMs;
            }
        }

        public void ResetCooldowns()
        {
            lock (this.mappingLock)
            {
                this.lastCastTimesMs.Clear();
            }
        }

        public void AddKeyToBuff(EffectStatusIDs status, Key key)
        {
            lock (this.mappingLock)
            {
                if (this.buffMapping.ContainsKey(status))
                {
                    this.buffMapping.Remove(status);
                }

                if (FormUtils.IsValidKey(key))
                {
                    this.buffMapping.Add(status, key);
                }
            }
        }

        public void ClearKeyMapping()
        {
            lock (this.mappingLock)
            {
                this.buffMapping.Clear();
                this.lastCastTimesMs.Clear();
            }
        }

        public Dictionary<EffectStatusIDs, Key> GetBuffMappingSnapshot()
        {
            lock (this.mappingLock)
            {
                return new Dictionary<EffectStatusIDs, Key>(this.buffMapping);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
            ResetCooldowns();
        }

        public string GetConfiguration()
        {
            lock (this.mappingLock)
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public string GetActionName()
        {
            return ACTION_NAME_AUTOBUFF;
        }

        private void UseAutobuff(Client client, Key key)
        {
            if (key == Key.None || KeyHelper.IsAltPressed() || client == null)
            {
                return;
            }

            Keys formsKey = KeyHelper.ToFormsKeys(key);
            Interop.SendKeyDown(client.GetWindowHandle(), formsKey);
        }
    }
}
