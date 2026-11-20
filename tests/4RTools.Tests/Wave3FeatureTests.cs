using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Input;
using _4RTools.Forms;
using _4RTools.Model;
using _4RTools.Utils;
using Xunit;

namespace _4RTools.Tests
{
    [Collection("StaticStateTests")]
    public class Wave3FeatureTests : IDisposable
    {
        private readonly string tempDir;
        private readonly string originalProfileFolder;
        private readonly string originalServerFile;
        private readonly string originalSettingsFile;
        private readonly bool originalDarkMode;
        private readonly string originalLanguage;

        public Wave3FeatureTests()
        {
            this.tempDir = Path.Combine(Path.GetTempPath(), "4RToolsWave3Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);

            this.originalProfileFolder = AppConfig.ProfileFolder;
            this.originalServerFile = LocalServerManager.LocalServerFilePath;
            this.originalSettingsFile = AppSettings.SettingsFilePath;
            this.originalDarkMode = ThemeManager.IsDarkMode;
            this.originalLanguage = LocalizationManager.CurrentLanguage;

            AppConfig.ProfileFolder = Path.Combine(this.tempDir, "Profile") + Path.DirectorySeparatorChar;
            LocalServerManager.LocalServerFilePath = Path.Combine(this.tempDir, "supported_servers.json");
            AppSettings.SettingsFilePath = Path.Combine(this.tempDir, "settings.json");
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
        }

        public void Dispose()
        {
            ThemeManager.ApplyTheme(null, this.originalDarkMode);
            LocalizationManager.SetLanguage(this.originalLanguage);
            AppConfig.ProfileFolder = this.originalProfileFolder;
            LocalServerManager.LocalServerFilePath = this.originalServerFile;
            AppSettings.SettingsFilePath = this.originalSettingsFile;

            try
            {
                if (Directory.Exists(this.tempDir))
                {
                    Directory.Delete(this.tempDir, true);
                }
            }
            catch
            {
                // Ignore temp cleanup errors.
            }
        }

        [Fact]
        public void AutoBuff_ShouldCastBuff_EnforcesMinSpCooldownQuagmireAndDebuffs()
        {
            AutoBuff autobuff = new AutoBuff();
            autobuff.minSpPercent = 30;
            autobuff.buffCooldownMs = 1500;
            autobuff.pauseOnDebuff = true;

            // Below minimum SP% should block cast.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 290, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 300, 1000));

            // Incapacitating debuff should block when pauseOnDebuff is enabled.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, true, 900, 1000));
            autobuff.pauseOnDebuff = false;
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, true, 900, 1000));

            // Quagmire blocks INC_AGI but still allows BLESSING.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.INC_AGI, 1000, true, false, 900, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, true, false, 900, 1000));

            // Per-buff cooldown blocks only the buff recently cast until cooldown elapses.
            autobuff.RecordBuffCast(EffectStatusIDs.BLESSING, 1000);
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1500, false, false, 900, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.INC_AGI, 1500, false, false, 900, 1000));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 2500, false, false, 900, 1000));

            // ResetCooldowns clears tracked cast times immediately.
            autobuff.RecordBuffCast(EffectStatusIDs.BLESSING, 3000);
            autobuff.ResetCooldowns();
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 3100, false, false, 900, 1000));
        }

        [Fact]
        public void KeybindConflictDetector_DetectsRealConflictsAndAllowsSharedStatusCure()
        {
            Profile profile = new Profile("WoE");
            profile.Autopot.hpKey = Key.F1;
            profile.AHK.AddAHKEntry("chkF1", new KeyConfig(Key.F1, true));

            profile.Autobuff.AddKeyToBuff(EffectStatusIDs.INC_AGI, Key.F2);
            profile.Autobuff.AddKeyToBuff(EffectStatusIDs.BLESSING, Key.F2);

            // Sharing a cure key across multiple statuses in StatusRecovery is intentional (e.g. Panacea).
            profile.StatusRecovery.AddKeyToBuff(EffectStatusIDs.POISON, Key.F5);
            profile.StatusRecovery.AddKeyToBuff(EffectStatusIDs.SILENCE, Key.F5);

            List<KeybindConflict> conflicts = KeybindConflictDetector.DetectConflicts(profile);

            Assert.Equal(2, conflicts.Count);
            Assert.Contains(conflicts, c => c.Key == Key.F1);
            Assert.Contains(conflicts, c => c.Key == Key.F2);
            Assert.DoesNotContain(conflicts, c => c.Key == Key.F5);

            string ptReport = KeybindConflictDetector.FormatReport(profile.Name, conflicts);
            Assert.Contains("2 conflito(s)", ptReport);
            Assert.Contains("[F1]", ptReport);
            Assert.Contains("[F2]", ptReport);

            string ptClean = KeybindConflictDetector.FormatReport("Limpo", new List<KeybindConflict>());
            Assert.Contains("Nenhum conflito", ptClean);

            LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
            string enReport = KeybindConflictDetector.FormatReport(profile.Name, conflicts);
            Assert.Contains("Found 2 keybind conflict(s)", enReport);
            string enClean = KeybindConflictDetector.FormatReport("CleanProfile", new List<KeybindConflict>());
            Assert.Contains("No keybind conflicts detected", enClean);
        }

        [Fact]
        public void ProfileBackupManager_ExportAndImportZip_RoundTripsProfilesServersAndSettings()
        {
            ProfileSingleton.Create("Default");
            ProfileSingleton.Create("BioChem");
            ProfileSingleton.GetCurrent().Autobuff.minSpPercent = 25;
            ProfileSingleton.GetCurrent().Autobuff.buffCooldownMs = 1750;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().Autobuff);

            LocalServerManager.AddServer("00E12345", "00F54321", "customro");
            AppSettings.Save(new AppSettings
            {
                DarkMode = true,
                AlwaysOnTop = true,
                ShowMiniHud = true,
                MiniHudX = 320,
                MiniHudY = 140,
                Language = LocalizationManager.LANG_PT_BR
            });

            string backupZip = Path.Combine(this.tempDir, "full-backup.zip");
            int exportedCount = ProfileBackupManager.ExportBackupZip(backupZip);
            Assert.True(exportedCount >= 4);
            Assert.True(File.Exists(backupZip));

            // Wipe local files to simulate moving to a fresh PC.
            Directory.Delete(AppConfig.ProfileFolder, true);
            File.Delete(LocalServerManager.LocalServerFilePath);
            File.Delete(AppSettings.SettingsFilePath);

            BackupRestoreResult result = ProfileBackupManager.ImportBackupZip(backupZip);
            Assert.Equal(2, result.ProfilesRestored);
            Assert.True(result.ServersRestored);
            Assert.True(result.SettingsRestored);

            ProfileSingleton.Load("BioChem");
            Assert.Equal(25, ProfileSingleton.GetCurrent().Autobuff.minSpPercent);
            Assert.Equal(1750, ProfileSingleton.GetCurrent().Autobuff.buffCooldownMs);

            List<ClientDTO> restoredServers = LocalServerManager.GetLocalClients();
            Assert.Contains(restoredServers, s => s.name == "customro" && s.hpAddress == "00E12345");

            AppSettings restoredSettings = AppSettings.Load();
            Assert.True(restoredSettings.DarkMode);
            Assert.True(restoredSettings.ShowMiniHud);
            Assert.Equal(320, restoredSettings.MiniHudX);
            Assert.Equal(140, restoredSettings.MiniHudY);
            Assert.Equal(LocalizationManager.LANG_PT_BR, restoredSettings.Language);
        }

        [Fact]
        public void ProfileBackupManager_ImportBackupZip_RejectsZipSlipPathTraversal()
        {
            string badZip = Path.Combine(this.tempDir, "malicious.zip");
            using (FileStream fs = new FileStream(badZip, FileMode.Create, FileAccess.ReadWrite))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                ZipArchiveEntry evil = archive.CreateEntry("Profile/../../evil.json");
                using (StreamWriter sw = new StreamWriter(evil.Open(), Encoding.UTF8))
                {
                    sw.Write("{\"Name\":\"Evil\"}");
                }
            }

            Assert.Throws<InvalidDataException>(() => ProfileBackupManager.ImportBackupZip(badZip));
        }

        [Fact]
        public void Client_TestMemoryAddresses_ValidatesInputsAndHandlesNonRunningProcess()
        {
            ServerAddressTestResult emptyProc = Client.TestMemoryAddresses("", "00E12345", "00F54321");
            Assert.False(emptyProc.Success);
            Assert.Contains("vazio", emptyProc.Message, StringComparison.OrdinalIgnoreCase);

            ServerAddressTestResult badHex = Client.TestMemoryAddresses("ragexe", "ZZZZ", "00F54321");
            Assert.False(badHex.Success);
            Assert.Contains("Endereço HP", badHex.Message);

            ServerAddressTestResult missingProc = Client.TestMemoryAddresses("NonExistentROProcess_4RTools_Test", "00E12345", "00F54321");
            Assert.False(missingProc.Success);
            Assert.Contains("não está em execução", missingProc.Message);
        }

        [Fact]
        public void LocalizationManager_TranslatesControlTreeBetweenPtBrAndEnUs()
        {
            using (System.Windows.Forms.Form form = new System.Windows.Forms.Form())
            using (System.Windows.Forms.Button btn = new System.Windows.Forms.Button())
            using (System.Windows.Forms.CheckBox chk = new System.Windows.Forms.CheckBox())
            using (System.Windows.Forms.TextBox input = new System.Windows.Forms.TextBox())
            using (System.Windows.Forms.DataGridView grid = new System.Windows.Forms.DataGridView())
            {
                form.Text = "Add New Server";
                btn.Text = "Save";
                chk.Text = "Dark Mode";
                input.Text = "Save"; // User input must never be overwritten.
                grid.Columns.Add("colName", "HP Address");

                form.Controls.Add(btn);
                form.Controls.Add(chk);
                form.Controls.Add(input);
                form.Controls.Add(grid);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
                LocalizationManager.ApplyToControlTree(form);

                Assert.Equal("Adicionar Novo Servidor", form.Text);
                Assert.Equal("Salvar", btn.Text);
                Assert.Equal("Modo Escuro", chk.Text);
                Assert.Equal("Save", input.Text);
                Assert.Equal("Endereço HP", grid.Columns[0].HeaderText);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                LocalizationManager.ApplyToControlTree(form);

                Assert.Equal("Add New Server", form.Text);
                Assert.Equal("Save", btn.Text);
                Assert.Equal("Dark Mode", chk.Text);
                Assert.Equal("HP Address", grid.Columns[0].HeaderText);
            }
        }

        [Fact]
        public void MiniHudForm_UpdatesStatusIdentityVitalsAndThemesWithoutError()
        {
            using (MiniHudForm hud = new MiniHudForm())
            {
                hud.SetSavedPosition(-1, -1);
                hud.ApplyHudTheme(true);
                hud.UpdateStatus(true, false);
                hud.UpdateStatus(true, true);
                hud.UpdateStatus(false, false);
                hud.UpdateIdentity("LordKnight", "ArthurRO");
                hud.UpdateVitals(8500, 10000, 400, 800);
                hud.ClearVitals();
                hud.ApplyHudTheme(false);

                Assert.True(hud.TopMost);
                Assert.False(hud.ShowInTaskbar);
            }
        }

        [Fact]
        public void Profile_SkillTimers4To6_PersistLoadAndDetectConflicts()
        {
            ProfileSingleton.Create("TimerTest");
            Profile current = ProfileSingleton.GetCurrent();
            current.AutoRefreshSpammer4.RefreshKey = Key.F4;
            current.AutoRefreshSpammer4.RefreshDelay = 12;
            current.AutoRefreshSpammer5.RefreshKey = Key.F5;
            current.AutoRefreshSpammer5.RefreshDelay = 30;
            current.AutoRefreshSpammer6.RefreshKey = Key.F4; // Intentional duplicate with Timer 4 for conflict test
            current.AutoRefreshSpammer6.RefreshDelay = 60;

            ProfileSingleton.SetConfiguration(current.AutoRefreshSpammer4);
            ProfileSingleton.SetConfiguration(current.AutoRefreshSpammer5);
            ProfileSingleton.SetConfiguration(current.AutoRefreshSpammer6);

            ProfileSingleton.Load("TimerTest");
            Profile reloaded = ProfileSingleton.GetCurrent();

            Assert.Equal(Key.F4, reloaded.AutoRefreshSpammer4.RefreshKey);
            Assert.Equal(12, reloaded.AutoRefreshSpammer4.RefreshDelay);
            Assert.Equal(Key.F5, reloaded.AutoRefreshSpammer5.RefreshKey);
            Assert.Equal(30, reloaded.AutoRefreshSpammer5.RefreshDelay);
            Assert.Equal(Key.F4, reloaded.AutoRefreshSpammer6.RefreshKey);
            Assert.Equal(60, reloaded.AutoRefreshSpammer6.RefreshDelay);

            List<KeybindConflict> conflicts = KeybindConflictDetector.DetectConflicts(reloaded);
            Assert.Contains(conflicts, c => c.Key == Key.F4);

            // Legacy profile JSON without AutoRefreshSpammer04-06 must still load cleanly with defaults.
            string legacyPath = ProfileSingleton.GetProfilePath("LegacyTimers");
            File.WriteAllText(legacyPath, "{\"Name\":\"LegacyTimers\"}");
            ProfileSingleton.Load("LegacyTimers");
            Assert.Equal(Key.None, ProfileSingleton.GetCurrent().AutoRefreshSpammer4.RefreshKey);
            Assert.Equal(5, ProfileSingleton.GetCurrent().AutoRefreshSpammer5.RefreshDelay);
            Assert.Equal("AutoRefreshSpammer06", ProfileSingleton.GetCurrent().AutoRefreshSpammer6.GetActionName());
        }

        [Fact]
        public void ModernControls_ThemeAndLocalization_UpdateCorrectly()
        {
            using (System.Windows.Forms.Form shell = new System.Windows.Forms.Form())
            using (System.Windows.Forms.Panel sidebar = new System.Windows.Forms.Panel())
            using (System.Windows.Forms.Label groupLabel = new System.Windows.Forms.Label())
            using (ModernCardPanel card = new ModernCardPanel())
            using (SidebarNavButton navBtn = new SidebarNavButton("spammer", "Skill Spammer", SidebarIconKind.Spammer))
            using (SegmentedTabButton tabBtn = new SegmentedTabButton("Autopot"))
            using (StatusDot dot = new StatusDot())
            {
                shell.Tag = ThemeManager.TAG_CANVAS;
                sidebar.Tag = ThemeManager.TAG_SIDEBAR;
                groupLabel.Text = "COMBAT & AUTOMATION";
                groupLabel.Tag = ThemeManager.TAG_MUTED_TEXT;

                navBtn.IsActive = true;
                tabBtn.IsActive = true;
                dot.IsConnected = true;
                dot.CustomColor = System.Drawing.Color.FromArgb(34, 197, 94);

                sidebar.Controls.Add(groupLabel);
                sidebar.Controls.Add(navBtn);
                card.Controls.Add(tabBtn);
                card.Controls.Add(dot);
                shell.Controls.Add(sidebar);
                shell.Controls.Add(card);

                ThemeManager.ApplyTheme(shell, true);
                Assert.Equal(System.Drawing.Color.FromArgb(11, 7, 1), shell.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(18, 12, 3), sidebar.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(30, 21, 6), card.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(89, 66, 20), card.BorderColor);

                ThemeManager.ApplyTheme(shell, false);
                Assert.Equal(System.Drawing.Color.FromArgb(242, 232, 204), shell.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(250, 244, 224), sidebar.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(253, 248, 232), card.BackColor);
                Assert.Equal(System.Drawing.Color.FromArgb(212, 192, 152), card.BorderColor);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
                LocalizationManager.ApplyToControlTree(shell);
                Assert.Equal("COMBATE & AUTOMAÇÃO", groupLabel.Text);
                Assert.Equal("Spam de Skills", navBtn.Text);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                LocalizationManager.ApplyToControlTree(shell);
                Assert.Equal("COMBAT & AUTOMATION", groupLabel.Text);
                Assert.Equal("Skill Spammer", navBtn.Text);
            }
        }

        [Fact]
        public void LocalizationManager_AllModernSectionsHaveTitlesAndSubtitlesInPtAndEn()
        {
            string[] sectionKeys = new string[]
            {
                "spammer", "debuff", "buff_skills", "buff_stuffs", "timers",
                "macro_switch", "macro_songs", "atkdef", "profiles", "servers"
            };

            string[] langs = new string[] { LocalizationManager.LANG_PT_BR, LocalizationManager.LANG_EN_US };
            foreach (string lang in langs)
            {
                LocalizationManager.SetLanguage(lang);
                foreach (string key in sectionKeys)
                {
                    string titleKey = "section." + key + ".title";
                    string subtitleKey = "section." + key + ".subtitle";
                    string title = LocalizationManager.T(titleKey);
                    string subtitle = LocalizationManager.T(subtitleKey);

                    Assert.False(string.IsNullOrWhiteSpace(title));
                    Assert.False(string.IsNullOrWhiteSpace(subtitle));
                    Assert.NotEqual(titleKey, title);
                    Assert.NotEqual(subtitleKey, subtitle);
                }

                Assert.NotEqual("footer.powered", LocalizationManager.T("footer.powered"));
                Assert.NotEqual("sidebar.status_on", LocalizationManager.T("sidebar.status_on"));
                Assert.NotEqual("sidebar.status_chat", LocalizationManager.T("sidebar.status_chat"));
                Assert.NotEqual("sidebar.status_off", LocalizationManager.T("sidebar.status_off"));
            }
        }

        [Fact]
        public void LocalizationManager_TranslatesAllBuffSkillsItemsAndDebuffsWithBilingualSearch()
        {
            List<Buff> allBuffs = new List<Buff>();
            allBuffs.AddRange(Buff.GetArcherSkills());
            allBuffs.AddRange(Buff.GetSwordmanSkill());
            allBuffs.AddRange(Buff.GetMageSkills());
            allBuffs.AddRange(Buff.GetMerchantSkills());
            allBuffs.AddRange(Buff.GetThiefSkills());
            allBuffs.AddRange(Buff.GetAcolyteSkills());
            allBuffs.AddRange(Buff.GetNinjaSkills());
            allBuffs.AddRange(Buff.GetTaekwonSkills());
            allBuffs.AddRange(Buff.GetGunsSkills());
            allBuffs.AddRange(Buff.GetSummonerSkills());
            allBuffs.AddRange(Buff.GetSoulAsceticSkills());
            allBuffs.AddRange(Buff.GetNightWatchSkills());
            allBuffs.AddRange(Buff.GetHyperNoviceSkills());
            allBuffs.AddRange(Buff.GetPotionsBuffs());
            allBuffs.AddRange(Buff.GetElementalsBuffs());
            allBuffs.AddRange(Buff.GetFoodBuffs());
            allBuffs.AddRange(Buff.GetBoxesBuffs());
            allBuffs.AddRange(Buff.GetScrollBuffs());
            allBuffs.AddRange(Buff.GetETCBuffs());
            allBuffs.AddRange(Buff.GetCandiesBuffs());
            allBuffs.AddRange(Buff.GetEXPBuffs());
            allBuffs.AddRange(Buff.GetHomunculusBuffs());
            allBuffs.AddRange(Buff.GetDebuffs());

            Assert.True(allBuffs.Count >= 260);
            foreach (Buff buff in allBuffs)
            {
                Assert.True(LocalizationManager.HasBuffTranslation(buff.name), "Missing translation for buff: " + buff.name);
            }

            // PT-BR mode shows "Portuguese (English)" using official bRO / RO LATAM names and avoids duplicating identical names.
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Assert.Equal("Poção da Fúria Selvagem (Berserk Potion)", LocalizationManager.GetBuffDisplayName("Berserk Potion"));
            Assert.Equal("Concentrar (Concentration)", LocalizationManager.GetBuffDisplayName("Concentration"));
            Assert.Equal("Caminho do Vento (Wind Walk)", LocalizationManager.GetBuffDisplayName("Wind Walk"));
            Assert.Equal("Proteção da Vanguarda (Vanguard Force)", LocalizationManager.GetBuffDisplayName("Vanguard Force"));
            Assert.Equal("Determinação (Vigor)", LocalizationManager.GetBuffDisplayName("Vigor"));
            Assert.Equal("Espada Alada (Servant Weapon)", LocalizationManager.GetBuffDisplayName("Servant Weapon"));
            Assert.Equal("Carrasco Sombrio (Shadow Exceed)", LocalizationManager.GetBuffDisplayName("Shadow Exceed"));
            Assert.Equal("Mantra da Energia (Sincere Faith)", LocalizationManager.GetBuffDisplayName("Sincere Faith"));
            Assert.Equal("Mantra da Saúde (Firm Faith)", LocalizationManager.GetBuffDisplayName("Firm Faith"));
            Assert.Equal("Mantra da Força (Powerful Faith)", LocalizationManager.GetBuffDisplayName("Powerful Faith"));
            Assert.Equal("Auréola do Poder (First Faith Power)", LocalizationManager.GetBuffDisplayName("First Faith Power"));
            Assert.Equal("Propulsão do Carrinho (Cart Boost - Genetic)", LocalizationManager.GetBuffDisplayName("Cart Boost (Genetic)"));
            Assert.Equal("Energético Físico (Red Herb Activator)", LocalizationManager.GetBuffDisplayName("Red Herb Activator"));
            Assert.Equal("Churrasco de Selvagem +20 FOR (Savage BBQ +20 STR)", LocalizationManager.GetBuffDisplayName("3RD STR Food"));
            Assert.Equal("Núcleo Golem (Golem Stone - GvG Golem)", LocalizationManager.GetBuffDisplayName("Battle Manual (GvG GOLEM)"));
            Assert.Equal("Taça da Felicidade (Sweets Strawberry Parfait)", LocalizationManager.GetBuffDisplayName("Sweets Strawberry Parfait"));
            Assert.Equal("Escritos Elementais (Undead Element Scroll)", LocalizationManager.GetBuffDisplayName("Undead Element Scroll"));
            Assert.Equal("Atordoamento (Stun)", LocalizationManager.GetBuffDisplayName("STUN"));
            Assert.Equal("Ilimitar (Unlimit)", LocalizationManager.GetBuffDisplayName("Ilimitar"));
            Assert.Equal("Fúria Interior (Fury)", LocalizationManager.GetBuffDisplayName("Fury "));
            Assert.Equal("Magnificat", LocalizationManager.GetBuffDisplayName("Magnificat"));
            Assert.Equal("Glória", LocalizationManager.GetBuffDisplayName("Gloria"));
            Assert.Equal("Guerrilheiro", LocalizationManager.TranslateText("Night Watch"));
            Assert.Equal("Asceta das Almas", LocalizationManager.TranslateText("Soul Ascetic"));

            // EN-US mode shows clean English names (including entries originally written in Portuguese).
            LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
            Assert.Equal("Berserk Potion", LocalizationManager.GetBuffDisplayName("Berserk Potion"));
            Assert.Equal("Unlimit", LocalizationManager.GetBuffDisplayName("Ilimitar"));
            Assert.Equal("Bubble Gum", LocalizationManager.GetBuffDisplayName("Goma de Mascar"));
            Assert.Equal("Stun", LocalizationManager.GetBuffDisplayName("STUN"));
            Assert.Equal("Night Watch", LocalizationManager.TranslateText("Guerrilheiro"));
            Assert.Equal("Soul Ascetic", LocalizationManager.TranslateText("Asceta das Almas"));

            // Bilingual, accent-insensitive search matches both PT-BR and EN-US terms.
            Assert.True(LocalizationManager.MatchesBuffSearch("Berserk Potion", "pocao"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Berserk Potion", "furia"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Berserk Potion", "berserk"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Concentration", "concentrar"));
            Assert.True(LocalizationManager.MatchesBuffSearch("First Faith Power", "aureola"));
            Assert.True(LocalizationManager.MatchesBuffSearch("3RD DEX Food", "cha gelado"));
            Assert.True(LocalizationManager.MatchesBuffSearch("3RD DEX Food", "siroma"));
            Assert.True(LocalizationManager.MatchesBuffSearch("STUN", "atordoamento"));
            Assert.True(LocalizationManager.MatchesBuffSearch("STUN", "stun"));
            Assert.False(LocalizationManager.MatchesBuffSearch("Berserk Potion", "Magnificat"));

            // BuffRenderer updates tooltips live when switching languages and matches group titles bilingually.
            using (System.Windows.Forms.GroupBox gp = new System.Windows.Forms.GroupBox())
            using (System.Windows.Forms.ToolTip tip = new System.Windows.Forms.ToolTip())
            {
                gp.Text = "Poções";
                List<BuffContainer> containers = new List<BuffContainer>
                {
                    new BuffContainer(gp, new List<Buff>
                    {
                        new Buff("Berserk Potion", EffectStatusIDs.BERSERK_POTION, null)
                    })
                };

                LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
                BuffRenderer renderer = new BuffRenderer("Autobuff", containers, tip);
                renderer.doRender(0);

                System.Windows.Forms.Control icon = gp.Controls["pbox" + (int)EffectStatusIDs.BERSERK_POTION];
                Assert.NotNull(icon);
                Assert.Equal("Poção da Fúria Selvagem (Berserk Potion)", tip.GetToolTip(icon));

                // Searching English group name "potions" should match Portuguese GroupBox "Poções".
                renderer.ApplyFilter("potions");
                Assert.True(icon.Visible);

                LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
                renderer.RefreshLocalizedTooltips();
                Assert.Equal("Berserk Potion", tip.GetToolTip(icon));
            }
        }

        [Fact]
        public void KeybindConflictDetector_UsesLocalizedOfficialBroBuffNamesInPtAndEn()
        {
            Profile profile = new Profile("Bilingue");
            profile.Autobuff.AddKeyToBuff(EffectStatusIDs.CONCENTRATION, Key.F3);
            profile.Autobuff.AddKeyToBuff(EffectStatusIDs.AWAKENING_POTION, Key.F3);

            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            List<KeybindConflict> ptConflicts = KeybindConflictDetector.DetectConflicts(profile);
            string ptReport = KeybindConflictDetector.FormatReport(profile.Name, ptConflicts);
            Assert.Contains("Autobuff (Concentrar (Concentration))", ptReport);
            Assert.Contains("Autobuff (Poção do Despertar (Awakening Potion))", ptReport);
            Assert.DoesNotContain("AWAKENING_POTION", ptReport);

            LocalizationManager.SetLanguage(LocalizationManager.LANG_EN_US);
            List<KeybindConflict> enConflicts = KeybindConflictDetector.DetectConflicts(profile);
            string enReport = KeybindConflictDetector.FormatReport(profile.Name, enConflicts);
            Assert.Contains("Autobuff (Concentration)", enReport);
            Assert.Contains("Autobuff (Awakening Potion)", enReport);
            Assert.DoesNotContain("AWAKENING_POTION", enReport);
        }

        [Fact]
        public void EmbeddedServers_AlwaysIncludeAureumROWithExpectedAddresses()
        {
            string raw = ClientUpdaterForm.LoadResourceServerFile();
            List<ClientDTO> servers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ClientDTO>>(raw);

            Assert.NotNull(servers);
            ClientDTO aureum = Assert.Single(servers);
            Assert.Equal("AureumRO", aureum.name);
            Assert.Equal("0x015FF908", aureum.hpAddress);
            Assert.Equal("0x01602568", aureum.nameAddress);
            Assert.True(ClientUpdaterForm.IsExclusiveServer(aureum));
            Assert.False(ClientUpdaterForm.IsExclusiveServer(new ClientDTO("OtherRO", "Other", "0x01", "0x02")));
            Assert.Contains("iOraculo/AureumRO-4Tools", AppConfig._4RClientsURL);
        }

        [Fact]
        public void ThemeManager_StripLightBackground_RemovesWhiteBackdropAndPreservesSubjectPixels()
        {
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.White);
                    }
                }

                // Draw a solid gold/green subject in the center with a white inner highlight pixel.
                for (int y = 4; y <= 11; y++)
                {
                    for (int x = 4; x <= 11; x++)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(255, 212, 175, 55));
                    }
                }
                bmp.SetPixel(7, 7, System.Drawing.Color.White);

                System.Drawing.Bitmap cleaned = ThemeManager.StripLightBackground(bmp) as System.Drawing.Bitmap;
                Assert.NotNull(cleaned);
                Assert.Equal(0, cleaned.GetPixel(0, 0).A);
                Assert.Equal(0, cleaned.GetPixel(15, 15).A);
                Assert.Equal(255, cleaned.GetPixel(5, 5).A);
                // Inner white highlight enclosed by subject must NOT be removed.
                Assert.Equal(255, cleaned.GetPixel(7, 7).A);
            }
        }

        [Fact]
        public void Client_IsTownMapAndUseBuffsInTownSetting_WorkCorrectly()
        {
            Assert.True(Client.IsTownMap("prontera.rsw"));
            Assert.True(Client.IsTownMap("Geffen"));
            Assert.True(Client.IsTownMap("moc_para01.rsw"));
            Assert.True(Client.IsTownMap("payon_in01.rsw"));
            Assert.False(Client.IsTownMap("prt_fild08.rsw"));
            Assert.False(Client.IsTownMap("gl_knt02.rsw"));
            Assert.False(Client.IsTownMap("abyss_03.rsw"));
            Assert.False(Client.IsTownMap(""));

            AppSettings defaults = AppSettings.Load();
            Assert.False(defaults.UseBuffsInTown);

            defaults.UseBuffsInTown = true;
            AppSettings.Save(defaults);
            Assert.True(AppSettings.Load().UseBuffsInTown);
        }

        [Fact]
        public void AutoBuff_PauseWhenSitting_BlocksBuffsWhileSitting()
        {
            AutoBuff autobuff = new AutoBuff();
            Assert.True(autobuff.pauseWhenSitting);

            // While sitting (EFST_SIT), autobuff must not fire when pauseWhenSitting is enabled.
            Assert.False(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 1000, 1000, true));
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 1000, 1000, false));

            // Disabling pauseWhenSitting allows casting even while sitting.
            autobuff.pauseWhenSitting = false;
            Assert.True(autobuff.ShouldCastBuff(EffectStatusIDs.BLESSING, 1000, false, false, 1000, 1000, true));
        }

        [Fact]
        public void PreRenewalMode_FiltersOutThirdAndFourthClassSkillsAndGroups()
        {
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.CONCENTRATION));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.EDP));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.AURABLADE));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.EFST_GOSPEL));
            Assert.True(Buff.IsPreRenewalSkill(EffectStatusIDs.CARTBOOST));
            Assert.Equal(109u, (uint)EffectStatusIDs.EFST_GOSPEL);

            Buff gospel = Buff.GetSwordmanSkill().FirstOrDefault(b => b.effectStatusID == EffectStatusIDs.EFST_GOSPEL);
            Assert.NotNull(gospel);
            Assert.NotNull(gospel.icon);
            LocalizationManager.SetLanguage(LocalizationManager.LANG_PT_BR);
            Assert.Equal("Canto de Batalha (Battle Chant / Gospel)", LocalizationManager.GetBuffDisplayName(gospel.name));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "canto"));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "batalha"));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "gospel"));
            Assert.True(LocalizationManager.MatchesBuffSearch(gospel.name, "battle chant"));

            // Verify all Pre-Renewal Bard / Clown & Dancer / Gypsy songs, dances, and ensembles.
            EffectStatusIDs[] bardDancerIds = new EffectStatusIDs[]
            {
                EffectStatusIDs.EFST_WHISTLE,
                EffectStatusIDs.EFST_ASSASSINCROSS,
                EffectStatusIDs.EFST_APPLEIDUN,
                EffectStatusIDs.EFST_HUMMING,
                EffectStatusIDs.EFST_DONTFORGETME,
                EffectStatusIDs.EFST_FORTUNEKISS,
                EffectStatusIDs.EFST_SERVICEFORYOU,
                EffectStatusIDs.EFST_RICHMANKIM,
                EffectStatusIDs.EFST_ETERNALCHAOS,
                EffectStatusIDs.EFST_DRUMBATTLEFIELD,
                EffectStatusIDs.EFST_RINGNIBELUNGEN,
                EffectStatusIDs.EFST_ROKISWEIL,
                EffectStatusIDs.EFST_INTOABYSS,
                EffectStatusIDs.EFST_SIEGFRIED,
                EffectStatusIDs.EFST_MARIONETTE_MASTER,
                EffectStatusIDs.EFST_MOON,
                EffectStatusIDs.EFST_LONGING,
                EffectStatusIDs.EFST_HERMODE
            };
            List<Buff> archerSkills = Buff.GetArcherSkills();
            foreach (EffectStatusIDs id in bardDancerIds)
            {
                Assert.True(Buff.IsPreRenewalSkill(id), "Missing Pre-Renewal flag for: " + id);
                Buff found = archerSkills.FirstOrDefault(b => b.effectStatusID == id);
                Assert.NotNull(found);
                Assert.NotNull(found.icon);
                Assert.True(LocalizationManager.HasBuffTranslation(found.name), "Missing translation for: " + found.name);
            }
            Assert.Equal("Dança Cigana (Service for You)", LocalizationManager.GetBuffDisplayName("Service for You"));
            Assert.Equal("Maçãs de Idun (The Apple of Idun)", LocalizationManager.GetBuffDisplayName("The Apple of Idun"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Service for You", "danca cigana"));
            Assert.True(LocalizationManager.MatchesBuffSearch("The Apple of Idun", "macas de idun"));
            Assert.True(LocalizationManager.MatchesBuffSearch("Invulnerable Siegfried", "siegfried"));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.UNLIMIT));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.ENCHANT_BLADE));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.EFST_HIDDEN_CARD));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.EFST_SHRIMP));
            Assert.False(Buff.IsPreRenewalSkill(EffectStatusIDs.GN_CARTBOOST));

            using (System.Windows.Forms.GroupBox archerGp = new System.Windows.Forms.GroupBox { Text = "Archer Skills" })
            using (System.Windows.Forms.GroupBox doramGp = new System.Windows.Forms.GroupBox { Text = "Summoner" })
            using (System.Windows.Forms.ToolTip tip = new System.Windows.Forms.ToolTip())
            {
                List<BuffContainer> containers = new List<BuffContainer>
                {
                    new BuffContainer(archerGp, new List<Buff>
                    {
                        new Buff("Concentration", EffectStatusIDs.CONCENTRATION, null),
                        new Buff("Ilimitar", EffectStatusIDs.UNLIMIT, null)
                    }),
                    new BuffContainer(doramGp, new List<Buff>
                    {
                        new Buff("Bunch of Shrimp", EffectStatusIDs.EFST_SHRIMP, null)
                    })
                };

                BuffRenderer renderer = new BuffRenderer("Autobuff", containers, tip);
                renderer.doRender(0);
                renderer.SetPreRenewalOnly(true);

                Assert.True(archerGp.Visible);
                Assert.True(archerGp.Controls["pbox" + (int)EffectStatusIDs.CONCENTRATION].Visible);
                Assert.False(archerGp.Controls["pbox" + (int)EffectStatusIDs.UNLIMIT].Visible);
                Assert.False(doramGp.Visible);

                renderer.SetPreRenewalOnly(false);
                Assert.True(archerGp.Controls["pbox" + (int)EffectStatusIDs.UNLIMIT].Visible);
                Assert.True(doramGp.Visible);
            }
        }

        [Fact]
        public void MacroSwitchForm_StartsSimplifiedWithOneSlotAndExpandsDynamically()
        {
            ProfileSingleton.Create("SwitchUiTest");
            ProfileSingleton.Load("SwitchUiTest");

            Subject subject = new Subject();
            using (MacroSwitchForm form = new MacroSwitchForm(subject))
            {
                // Fresh profile starts clean: only Switch 1 visible, with 1 slot.
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(1, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.False(form.IsLaneVisible(3));

                // Adding slots expands up to 7 and removing collapses back down cleanly.
                form.AddSlotToLane(1);
                form.AddSlotToLane(1);
                Assert.Equal(3, form.GetVisibleSlotCount(1));

                form.RemoveLastSlotFromLane(1);
                Assert.Equal(2, form.GetVisibleSlotCount(1));

                // Adding extra switch lanes reveals Switch 2 on demand.
                Assert.True(form.AddNextSwitchLane());
                Assert.True(form.IsLaneVisible(2));
                Assert.Equal(1, form.GetVisibleSlotCount(2));

                // Removing Switch 2 hides it again.
                form.ClearLane(2, true);
                Assert.False(form.IsLaneVisible(2));

                // Loading a profile with existing bindings auto-expands only configured lanes/slots.
                ChainConfig lane1 = ProfileSingleton.GetCurrent().MacroSwitch.chainConfigs.First(c => c.id == 1);
                lane1.trigger = Key.F1;
                lane1.macroEntries["in1mac1"] = new MacroKey(Key.F1, 50);
                lane1.macroEntries["in2mac1"] = new MacroKey(Key.F2, 50);
                lane1.macroEntries["in3mac1"] = new MacroKey(Key.F3, 50);

                ChainConfig lane3 = ProfileSingleton.GetCurrent().MacroSwitch.chainConfigs.First(c => c.id == 3);
                lane3.trigger = Key.F5;
                lane3.macroEntries["in1mac3"] = new MacroKey(Key.F5, 50);
                lane3.macroEntries["in2mac3"] = new MacroKey(Key.F6, 50);

                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(3, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.True(form.IsLaneVisible(3));
                Assert.Equal(2, form.GetVisibleSlotCount(3));
            }
        }

        [Fact]
        public void ATKDEFForm_ContainsAllTenEquipmentSlotsForAtkAndDef()
        {
            ProfileSingleton.Create("AtkDefTenSlotsTest");
            ProfileSingleton.Load("AtkDefTenSlotsTest");

            Subject subject = new Subject();
            using (ATKDEFForm form = new ATKDEFForm(subject))
            {
                for (int i = 1; i <= 10; i++)
                {
                    System.Windows.Forms.Control[] defMatches = form.Controls.Find("inDef" + i, true);
                    System.Windows.Forms.Control[] atkMatches = form.Controls.Find("inAtk" + i, true);
                    Assert.Single(defMatches);
                    Assert.Single(atkMatches);
                }

                // Verify new Hat Top (inDef6) and Right Accessory (inAtk10) persist through PROFILE_CHANGED.
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem("inDef6", Key.F1, ATKDEFEnum.DEF);
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem("inAtk10", Key.F9, ATKDEFEnum.ATK);
                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));

                Assert.Equal("F1", form.Controls.Find("inDef6", true)[0].Text);
                Assert.Equal("F9", form.Controls.Find("inAtk10", true)[0].Text);
            }
        }

        [Fact]
        public void MacroSongForm_StartsSimplifiedWithOneSongSlotAndExpandsDynamically()
        {
            ProfileSingleton.Create("SongUiTest");
            ProfileSingleton.Load("SongUiTest");

            Subject subject = new Subject();
            using (MacroSongForm form = new MacroSongForm(subject))
            {
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(1, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.False(form.IsLaneVisible(3));

                form.AddSlotToLane(1);
                form.AddSlotToLane(1);
                form.AddSlotToLane(1);
                Assert.Equal(4, form.GetVisibleSlotCount(1));

                form.RemoveLastSlotFromLane(1);
                Assert.Equal(3, form.GetVisibleSlotCount(1));

                Assert.True(form.AddNextSongLane());
                Assert.True(form.IsLaneVisible(2));
                Assert.Equal(1, form.GetVisibleSlotCount(2));

                form.ResetLane(2, true);
                Assert.False(form.IsLaneVisible(2));

                ChainConfig song1 = ProfileSingleton.GetCurrent().SongMacro.chainConfigs.First(c => c.id == 1);
                song1.trigger = Key.F1;
                song1.macroEntries["in1mac1"] = new MacroKey(Key.F2, 50);
                song1.macroEntries["in2mac1"] = new MacroKey(Key.F3, 50);
                song1.macroEntries["in5mac1"] = new MacroKey(Key.F4, 50);

                ChainConfig song3 = ProfileSingleton.GetCurrent().SongMacro.chainConfigs.First(c => c.id == 3);
                song3.trigger = Key.F6;
                song3.macroEntries["in1mac3"] = new MacroKey(Key.F7, 50);

                subject.Notify(new _4RTools.Utils.Message(MessageCode.PROFILE_CHANGED, null));
                Assert.True(form.IsLaneVisible(1));
                Assert.Equal(5, form.GetVisibleSlotCount(1));
                Assert.False(form.IsLaneVisible(2));
                Assert.True(form.IsLaneVisible(3));
                Assert.Equal(1, form.GetVisibleSlotCount(3));
            }
        }
    }
}
