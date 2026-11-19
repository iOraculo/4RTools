using System;
using System.Collections.Generic;
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

        private readonly object mappingLock = new object();
        private _4RThread thread;
        public int delay { get; set; } = 1;
        public Dictionary<EffectStatusIDs, Key> buffMapping = new Dictionary<EffectStatusIDs, Key>();

        public void Start()
        {
            Stop();
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

            return new _4RThread(token =>
            {
                if (c.HasMemorySupport && c.IsProcessAlive() && !c.ShouldPauseAutomation())
                {
                    bool foundQuag = false;
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
                        }

                        if (missingBuffs.Count > 0 && c.ReadCurrentHp() >= Constants.MINIMUM_HP_TO_RECOVER)
                        {
                            foreach (KeyValuePair<EffectStatusIDs, Key> item in missingBuffs)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    break;
                                }

                                if (foundQuag && QuagmireBlockedBuffs.Contains(item.Key))
                                {
                                    continue;
                                }

                                UseAutobuff(c, item.Value);
                                if (!_4RThread.Sleep(10, token))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                _4RThread.Sleep(300, token);
            }, ACTION_NAME_AUTOBUFF);
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
