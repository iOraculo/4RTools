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
    }
}
