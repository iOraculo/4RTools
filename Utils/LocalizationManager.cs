using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public static class LocalizationManager
    {
        public const string LANG_PT_BR = "pt-BR";
        public const string LANG_EN_US = "en-US";

        private static string currentLanguage = LANG_PT_BR;

        // Exact-match dictionary mapping English Designer/UI strings to Portuguese (PT-BR).
        // Reverse lookup is generated automatically so switching PT-BR <-> EN-US live works seamlessly.
        private static readonly Dictionary<string, string> EnToPt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Container & Tabs
            { "Ragnarok Client", "Cliente Ragnarok" },
            { "Profile", "Perfil" },
            { "Character Name:", "Personagem:" },
            { "Dark Mode", "Modo Escuro" },
            { "Always on Top", "Sempre no Topo" },
            { "Mini HUD", "Mini HUD" },
            { "Join in our discord", "Entre no nosso Discord" },
            { "Powered by AuTHEntiC and MarkiinG", "Desenvolvido por AuTHEntiC e MarkiinG" },
            { "Skill Spammer", "Spam de Skills" },
            { "Debuff", "Debuffs" },
            { "Autobuff - Skills", "Autobuff - Skills" },
            { "Autobuff - Stuffs", "Autobuff - Itens" },
            { "Skill timers", "Timers de Skill" },
            { "Macro Switch", "Macro Troca" },
            { "Macro Songs", "Macro Músicas" },
            { "ATK x DEF Mode", "Modo ATK x DEF" },
            { "Profiles", "Perfis" },
            { "Servers", "Servidores" },

            // ToggleApplicationStateForm
            { "ON / OFF", "LIGAR / DESLIGAR" },
            { "Sound", "Som" },
            { "Focus", "Foco" },
            { "Chat", "Chat" },
            { "Close", "Fechar" },
            { "Press the key to start!", "Aperte a tecla p/ iniciar!" },
            { "Press the key to stop!", "Aperte a tecla p/ parar!" },
            { "Chat paused (Esc/Enter)", "Pausa de chat (Esc/Enter)" },
            { "Client closed!", "Cliente fechado!" },
            { "Please select the Ragnarok Client!!", "Selecione o cliente RO!" },

            // AutopotForm
            { "Delay", "Atraso" },

            // AHKForm
            { "AHK Configuration", "Configuração do Spammer" },
            { "Compatibility", "Compatibilidade" },
            { "Speed boost", "Velocidade Máx." },
            { "Key Config", "Config. Tecla" },
            { "Mouse Flick", "Mouse Flick" },
            { "No Shift", "Sem Shift" },
            { "Spammer Delay", "Atraso (Delay)" },
            { "Example", "Legenda" },
            { "With mouse click", "Com clique" },
            { "No mouse click", "Sem clique" },
            { "Deactivated", "Desativado" },
            { "Clear All Keys", "Limpar Teclas" },

            // DebuffRecoveryForm
            { "Status Recovery", "Recuperação de Status" },
            { "Status Effects", "Efeitos / Debuffs" },
            { "Enable Auto Stand (when forced to sit)", "Levantar automaticamente (quando sentado)" },
            { "Elvira Candy", "Doce de Elvira" },
            { "Cooldown", "Recarga" },

            // SkillAutoBuffForm & StuffAutoBuffForm
            { "Filter:", "Filtrar:" },
            { "Min SP%:", "SP Mín%:" },
            { "CD(ms):", "CD(ms):" },
            { "Clear Skills", "Limpar Skills" },
            { "Clear Stuffs", "Limpar Itens" },
            { "Archer Skills", "Arqueiro / Evoluções" },
            { "Swordsman Skills", "Espadachim / Evoluções" },
            { "Mage Skills", "Mago / Evoluções" },
            { "Merchant Skills", "Mercador / Evoluções" },
            { "Thief Skills", "Gatuno / Evoluções" },
            { "Acolyte Skills", "Noviço / Evoluções" },
            { "Taekwon Skills", "Taekwon / Evoluções" },
            { "Ninja Skills", "Ninja / Evoluções" },
            { "Gunslinger Skills", "Justiceiro / Evoluções" },
            { "Summoner", "Invocador (Doram)" },
            { "Hyper Novice", "Hiper Aprendiz" },
            { "Homunculus", "Homúnculo" },
            { "Potions", "Poções" },
            { "Elementals", "Elementais / Conversores" },
            { "Boxes / Speed / Status", "Caixas / Velocidade / Status" },
            { "Foods", "Comidas" },
            { "Scroll Buffs", "Pergaminhos" },
            { "ETC", "Outros (ETC)" },
            { "Candies", "Doces" },
            { "EXP", "EXP / Drop" },

            // SkillTimerForm
            { "Skill timer 1", "Timer de Skill 1" },
            { "Skill timer 2", "Timer de Skill 2" },
            { "Skill timer 3", "Timer de Skill 3" },
            { "Key", "Tecla" },
            { "sec", "seg" },

            // MacroSongForm & MacroSwitchForm
            { "Reset", "Resetar" },
            { "Switch 1", "Troca 1" },
            { "Switch 2", "Troca 2" },
            { "Switch 3", "Troca 3" },
            { "Switch 4", "Troca 4" },
            { "Switch 5", "Troca 5" },
            { "Keys:", "Teclas:" },
            { "Delays(ms):", "Atrasos(ms):" },
            { "Click", "Clique" },

            // ATKDEFForm
            { "ATK Switch", "Troca ATK" },
            { "DEF Switch", "Troca DEF" },
            { "Spammer Key", "Tecla Spammer" },
            { "Configuration", "Configuração" },
            { "Switch Delay", "Atraso da Troca" },

            // ProfileForm & EditProfileName
            { "Create a new profile", "Criar um novo perfil" },
            { "Create", "Criar" },
            { "Profile List", "Lista de Perfis" },
            { "Remove Selected Profile", "Remover" },
            { "Edit", "Editar" },
            { "Copy", "Copiar" },
            { "Export", "Exportar" },
            { "Import", "Importar" },
            { "Backup All (.zip)", "Backup (.zip)" },
            { "Restore (.zip)", "Restaurar (.zip)" },
            { "Check Key Conflicts", "Verificar Conflitos" },
            { "Auto-Switch by Character", "Troca por Personagem" },
            { "Enable Auto-Switch", "Ativar Troca Automática" },
            { "Use Active Character", "Usar Personagem Ativo" },
            { "Save", "Salvar" },
            { "Clear", "Limpar" },
            { "Auto-loads profile when this character is detected (while OFF).", "Carrega o perfil ao detectar este personagem (em OFF)." },
            { "Edit Profile Name", "Renomear Perfil" },
            { "New profile name:", "Novo nome do perfil:" },

            // ServersForm & AddServerForm
            { "Add Server", "Adicionar Servidor" },
            { "Add New Server", "Adicionar Novo Servidor" },
            { "Server Properties", "Propriedades do Servidor" },
            { "HP Address:", "Endereço HP:" },
            { "Name Address:", "Endereço Nome:" },
            { "Process Name:", "Nome do Processo:" },
            { "Test", "Testar" },
            { "Click Test while logged in to verify addresses.", "Clique em Testar logado no jogo para validar." },
            { "Name", "Nome" },
            { "HP Address", "Endereço HP" },
            { "Name Address", "Endereço Nome" },
            { "Delete", "Excluir" },

            // Updaters
            { "Updating 4RTools...", "Atualizando 4RTools..." },
            { "Updating Supported Servers...", "Atualizando servidores suportados..." }
        };

        private static readonly Dictionary<string, string> PtToEn = BuildReverseDictionary(EnToPt);

        // Keyed message table for dialogs, tooltips, reports, and dynamic text.
        private static readonly Dictionary<string, string> MessagesPt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Common titles
            { "title.warning", "Aviso" },
            { "title.error", "Erro" },
            { "title.confirm", "Confirmar" },
            { "title.message", "Mensagem" },
            { "title.yes", "Sim" },
            { "title.no", "Não" },

            // Container
            { "tooltip.dark_mode", "Alternar entre tema claro e escuro" },
            { "tooltip.mini_hud", "Exibir janela flutuante compacta com status e barras de HP/SP" },
            { "tooltip.top_most", "Manter o 4RTools sempre acima da janela do Ragnarok" },
            { "tooltip.language", "Alterar o idioma da interface (Português / Inglês)" },
            { "msg.client_unsupported", "Este cliente não é suportado. Apenas Spammers e Macros vão funcionar." },
            { "msg.profile_load_error", "Erro ao carregar o novo perfil.\nDetalhes: {0}" },

            // ToggleApplicationStateForm
            { "tooltip.focus_only", "Pausar automações quando a janela do Ragnarok não estiver em foco" },
            { "tooltip.chat_pause", "Pausa de Chat: aperte Enter no Ragnarok para pausar ao digitar (Esc ou Enter retoma; Autopot em F1–F24 continua ativo)" },
            { "status.press_to_start", "Aperte a tecla p/ iniciar!" },
            { "status.press_to_stop", "Aperte a tecla p/ parar!" },
            { "status.chat_paused", "Pausa de chat (Esc/Enter)" },
            { "status.client_closed", "Cliente fechado!" },
            { "status.select_client", "Selecione o cliente RO!" },
            { "tray.close", "Fechar" },

            // AHKForm
            { "msg.ahk_clear_confirm", "Limpar todas as teclas configuradas no Skill Spammer deste perfil?" },
            { "title.ahk_clear", "Limpar Teclas" },

            // Autobuff Forms
            { "tooltip.min_sp", "Pausar Autobuff quando o SP estiver abaixo desta % (0 = desativado)" },
            { "tooltip.buff_cd", "Tempo de recarga por buff em milissegundos (ex.: 1500ms evita uso duplo)" },
            { "msg.clear_skills_confirm", "Limpar todas as teclas de skills de Autobuff nesta aba?" },
            { "msg.clear_stuffs_confirm", "Limpar todas as teclas de itens de Autobuff nesta aba?" },

            // ProfileForm
            { "label.profile_prefix", "Perfil: {0}" },
            { "title.active_char", "Personagem Ativo" },
            { "msg.select_client_first", "Selecione um cliente Ragnarok suportado primeiro para ler o nome do personagem ativo." },
            { "msg.char_not_detected", "Não foi possível detectar o nome do personagem ativo no cliente Ragnarok." },
            { "title.char_binding", "Vínculo de Personagem" },
            { "msg.binding_cleared", "Vínculo de personagem removido para o perfil '{0}'." },
            { "msg.binding_saved", "Perfil '{0}' vinculado ao personagem '{1}'." },
            { "msg.binding_save_error", "Falha ao salvar vínculo de personagem: {0}" },
            { "msg.profile_select_delete", "Nenhum perfil selecionado! Selecione um perfil na lista para remover." },
            { "msg.profile_cannot_delete_default", "Não é possível remover o perfil Default!" },
            { "msg.profile_select_rename", "Selecione um perfil na lista para renomear." },
            { "msg.profile_cannot_rename_default", "Não é possível renomear o perfil Default!" },
            { "msg.profile_select_copy", "Selecione um perfil na lista para copiar." },
            { "msg.profile_cannot_copy_default", "Não é possível copiar o perfil Default!" },
            { "filter.profile_json", "Perfil 4RTools (*.json)|*.json" },
            { "title.export_profile", "Exportar Perfil" },
            { "msg.export_success", "Perfil exportado com sucesso!" },
            { "msg.export_error", "Falha ao exportar perfil: {0}" },
            { "title.import_profile", "Importar Perfil" },
            { "msg.import_success", "Perfil '{0}' importado com sucesso!" },
            { "msg.import_error", "Falha ao importar perfil: {0}" },
            { "filter.backup_zip", "Backup 4RTools (*.zip)|*.zip" },
            { "title.backup_all", "Backup Completo" },
            { "msg.backup_success", "Backup criado com sucesso ({0} arquivo(s) exportado(s))!" },
            { "msg.backup_error", "Falha ao criar backup: {0}" },
            { "title.restore_backup", "Restaurar Backup" },
            { "msg.restore_confirm", "Restaurar perfis, servidores customizados e configurações deste backup?\nPerfis existentes com o mesmo nome serão atualizados." },
            { "msg.restore_summary", "Backup restaurado!\nPerfis restaurados: {0}\nServidores customizados restaurados: {1}\nConfigurações restauradas: {2}" },
            { "msg.restore_error", "Falha ao restaurar backup: {0}" },
            { "title.key_conflicts", "Inspetor de Conflito de Teclas" },

            // ServersForm & AddServerForm
            { "msg.server_delete_confirm", "Tem certeza que deseja excluir este servidor?" },
            { "msg.server_deleted", "Servidor {0} excluído com sucesso!" },
            { "title.edit_server", "Editar Servidor {0}" },
            { "msg.server_added", "Servidor {0} adicionado com sucesso!" },
            { "msg.server_saved", "Servidor {0} salvo com sucesso!" },
            { "title.test_success", "Teste de Endereço OK" },
            { "title.test_result", "Teste de Endereço" },
            { "label.test_ok_inline", "OK: {0}\nHP {1}/{2}" },
            { "label.test_failed_inline", "Teste falhou. Veja detalhes." },
            { "label.connected", "Conectado" },

            // Client.TestMemoryAddresses
            { "test.empty_process", "O nome do processo não pode ficar vazio." },
            { "test.invalid_hp", "Endereço HP inválido. Informe um valor hexadecimal de 1 a 8 dígitos." },
            { "test.invalid_name", "Endereço Nome inválido. Informe um valor hexadecimal de 1 a 8 dígitos." },
            { "test.process_not_running", "O processo '{0}' não está em execução. Abra o cliente do jogo primeiro." },
            { "test.open_failed", "Falha ao abrir a memória do processo '{0}'. Tente executar o 4RTools como Administrador." },
            { "test.read_error", "Erro ao ler memória do processo: {0}" },
            { "test.ok_report", "Conexão OK!\nPersonagem: {0}\nHP: {1:N0} / {2:N0}\nSP: {3:N0} / {4:N0}\nBuffs ativos: {5}" },
            { "test.not_detected", "(não detectado)" },
            { "test.empty_values", "Os endereços retornaram 0/vazio (HP: {0}/{1}, Nome: vazio).\nVerifique se está logado em um mapa e se os endereços correspondem a este cliente." },

            // KeybindConflictDetector
            { "conflicts.none", "Nenhum conflito de tecla detectado no perfil '{0}'!" },
            { "conflicts.header", "Encontrado(s) {0} conflito(s) de tecla no perfil '{1}':" },
            { "usage.toggle", "Tecla Ligar/Desligar" },
            { "usage.autopot_hp", "Autopot (HP)" },
            { "usage.autopot_sp", "Autopot (SP)" },
            { "usage.ygg_hp", "Autopot Yggdrasil (HP)" },
            { "usage.ygg_sp", "Autopot Yggdrasil (SP)" },
            { "usage.ahk_click", "Spam de Skills (Com clique)" },
            { "usage.ahk_noclick", "Spam de Skills (Sem clique)" },
            { "usage.autobuff", "Autobuff ({0})" },
            { "usage.status_recovery", "Recuperação de Status" },
            { "usage.debuff_recovery", "Recuperação de Debuffs" },
            { "usage.skill_timer", "Timer de Skill {0}" },
            { "usage.atkdef_spam", "ATK/DEF Spammer" },
            { "usage.atkdef_atk", "ATK/DEF (Equip. ATK)" },
            { "usage.atkdef_def", "ATK/DEF (Equip. DEF)" },
            { "usage.song_macro", "Macro Músicas" },
            { "usage.macro_switch", "Macro Troca" },
            { "usage.macro_trigger", "{0} (Linha {1} Gatilho)" },
            { "usage.macro_instrument", "{0} (Instrumento)" },
            { "usage.macro_dagger", "{0} (Adaga)" },
            { "usage.macro_slot", "{0} (Linha {1} Slot)" }
        };

        private static readonly Dictionary<string, string> MessagesEn = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "title.warning", "Warning" },
            { "title.error", "Error" },
            { "title.confirm", "Confirm" },
            { "title.message", "Message" },
            { "title.yes", "Yes" },
            { "title.no", "No" },

            { "tooltip.dark_mode", "Switch between Light and Dark interface themes" },
            { "tooltip.mini_hud", "Show a compact floating overlay with status and HP/SP bars" },
            { "tooltip.top_most", "Keep the 4RTools window pinned above Ragnarok" },
            { "tooltip.language", "Switch interface language (Portuguese / English)" },
            { "msg.client_unsupported", "This client is not supported. Only Spammers and macro will work." },
            { "msg.profile_load_error", "Error while loading the new profile.\nDetails: {0}" },

            { "tooltip.focus_only", "Pause automations when the Ragnarok window is not focused" },
            { "tooltip.chat_pause", "Chat-Safe Pause: press Enter in Ragnarok to pause automations while typing (Esc or Enter resumes; F-key Autopot stays active)" },
            { "status.press_to_start", "Press the key to start!" },
            { "status.press_to_stop", "Press the key to stop!" },
            { "status.chat_paused", "Chat paused (Esc/Enter)" },
            { "status.client_closed", "Client closed!" },
            { "status.select_client", "Please select the Ragnarok Client!!" },
            { "tray.close", "Close" },

            { "msg.ahk_clear_confirm", "Clear all configured Skill Spammer keys in this profile?" },
            { "title.ahk_clear", "Clear All Keys" },

            { "tooltip.min_sp", "Pause Autobuff when SP is below this % (0 = disabled)" },
            { "tooltip.buff_cd", "Per-buff recast cooldown in milliseconds (e.g. 1500ms prevents double-use)" },
            { "msg.clear_skills_confirm", "Clear all skill autobuff keys on this tab?" },
            { "msg.clear_stuffs_confirm", "Clear all consumable autobuff keys on this tab?" },

            { "label.profile_prefix", "Profile: {0}" },
            { "title.active_char", "Active Character" },
            { "msg.select_client_first", "Select a supported Ragnarok client first to read the active character name." },
            { "msg.char_not_detected", "Could not detect an active character name from the Ragnarok client." },
            { "title.char_binding", "Character Binding" },
            { "msg.binding_cleared", "Character binding cleared for profile '{0}'." },
            { "msg.binding_saved", "Profile '{0}' bound to character '{1}'." },
            { "msg.binding_save_error", "Failed to save character binding: {0}" },
            { "msg.profile_select_delete", "No profile selected! To delete a profile, first select an option from the Profile list." },
            { "msg.profile_cannot_delete_default", "Cannot delete the Default profile!" },
            { "msg.profile_select_rename", "To rename a profile, first select one from the Profile list." },
            { "msg.profile_cannot_rename_default", "Cannot rename the Default profile!" },
            { "msg.profile_select_copy", "To copy a profile, first select one from the Profile list." },
            { "msg.profile_cannot_copy_default", "Cannot copy the Default profile!" },
            { "filter.profile_json", "4RTools Profile (*.json)|*.json" },
            { "title.export_profile", "Export Profile" },
            { "msg.export_success", "Profile exported successfully!" },
            { "msg.export_error", "Failed to export profile: {0}" },
            { "title.import_profile", "Import Profile" },
            { "msg.import_success", "Profile '{0}' imported successfully!" },
            { "msg.import_error", "Failed to import profile: {0}" },
            { "filter.backup_zip", "4RTools Backup (*.zip)|*.zip" },
            { "title.backup_all", "Backup All" },
            { "msg.backup_success", "Backup created successfully ({0} file(s) exported)!" },
            { "msg.backup_error", "Failed to create backup: {0}" },
            { "title.restore_backup", "Restore Backup" },
            { "msg.restore_confirm", "Restore profiles, custom servers, and settings from this backup?\nExisting profiles with the same name will be updated." },
            { "msg.restore_summary", "Backup restored!\nProfiles restored: {0}\nCustom servers restored: {1}\nApp settings restored: {2}" },
            { "msg.restore_error", "Failed to restore backup: {0}" },
            { "title.key_conflicts", "Keybind Conflict Inspector" },

            { "msg.server_delete_confirm", "Are you sure want to delete this Server?" },
            { "msg.server_deleted", "Server {0} successfully deleted!" },
            { "title.edit_server", "Edit Server {0}" },
            { "msg.server_added", "Server {0} successfully added!" },
            { "msg.server_saved", "Server {0} successfully saved!" },
            { "title.test_success", "Address Test Successful" },
            { "title.test_result", "Address Test" },
            { "label.test_ok_inline", "OK: {0}\nHP {1}/{2}" },
            { "label.test_failed_inline", "Test failed. Check details." },
            { "label.connected", "Connected" },

            { "test.empty_process", "Process name cannot be empty." },
            { "test.invalid_hp", "HP Address is invalid. Enter a valid 1-8 digit hex value." },
            { "test.invalid_name", "Name Address is invalid. Enter a valid 1-8 digit hex value." },
            { "test.process_not_running", "Process '{0}' is not currently running. Launch the game client first." },
            { "test.open_failed", "Failed to open process memory for '{0}'. Try running 4RTools as Administrator." },
            { "test.read_error", "Error reading process memory: {0}" },
            { "test.ok_report", "Connection OK!\nCharacter: {0}\nHP: {1:N0} / {2:N0}\nSP: {3:N0} / {4:N0}\nActive Buffs: {5}" },
            { "test.not_detected", "(not detected)" },
            { "test.empty_values", "Addresses read 0/empty (HP: {0}/{1}, Name: empty).\nMake sure you are logged in on a map and the hex addresses match this client." },

            { "conflicts.none", "No keybind conflicts detected in profile '{0}'!" },
            { "conflicts.header", "Found {0} keybind conflict(s) in profile '{1}':" },
            { "usage.toggle", "ON/OFF Toggle Hotkey" },
            { "usage.autopot_hp", "Autopot (HP)" },
            { "usage.autopot_sp", "Autopot (SP)" },
            { "usage.ygg_hp", "Yggdrasil Autopot (HP)" },
            { "usage.ygg_sp", "Yggdrasil Autopot (SP)" },
            { "usage.ahk_click", "Skill Spammer (Click)" },
            { "usage.ahk_noclick", "Skill Spammer (No-Click)" },
            { "usage.autobuff", "Autobuff ({0})" },
            { "usage.status_recovery", "Status Recovery" },
            { "usage.debuff_recovery", "Debuff Recovery" },
            { "usage.skill_timer", "Skill Timer {0}" },
            { "usage.atkdef_spam", "ATK/DEF Spammer" },
            { "usage.atkdef_atk", "ATK/DEF (ATK Equip)" },
            { "usage.atkdef_def", "ATK/DEF (DEF Equip)" },
            { "usage.song_macro", "Song Macro" },
            { "usage.macro_switch", "Macro Switch" },
            { "usage.macro_trigger", "{0} (Lane {1} Trigger)" },
            { "usage.macro_instrument", "{0} (Instrument)" },
            { "usage.macro_dagger", "{0} (Dagger)" },
            { "usage.macro_slot", "{0} (Lane {1} Slot)" }
        };

        public static string CurrentLanguage
        {
            get { return currentLanguage; }
        }

        public static bool IsPortuguese
        {
            get { return string.Equals(currentLanguage, LANG_PT_BR, StringComparison.OrdinalIgnoreCase); }
        }

        public static void SetLanguage(string language)
        {
            if (string.Equals(language, LANG_EN_US, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                currentLanguage = LANG_EN_US;
            }
            else
            {
                currentLanguage = LANG_PT_BR;
            }
        }

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            Dictionary<string, string> primary = IsPortuguese ? MessagesPt : MessagesEn;
            string value;
            if (primary.TryGetValue(key, out value))
            {
                return value;
            }

            if (MessagesEn.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(format, args);
        }

        public static string TranslateText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string translated;
            if (IsPortuguese)
            {
                if (EnToPt.TryGetValue(text, out translated))
                {
                    return translated;
                }
            }
            else
            {
                if (PtToEn.TryGetValue(text, out translated))
                {
                    return translated;
                }
            }

            return text;
        }

        public static void ApplyToControlTree(Control root)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            root.SuspendLayout();
            try
            {
                ApplyToControl(root);
            }
            finally
            {
                root.ResumeLayout(true);
            }
        }

        private static void ApplyToControl(Control control)
        {
            if (control == null || control.IsDisposed)
            {
                return;
            }

            // Never translate user-editable input boxes or dropdown items.
            if (!(control is TextBox) && !(control is ComboBox) && !(control is ListBox) && !(control is NumericUpDown))
            {
                if (!string.IsNullOrEmpty(control.Text))
                {
                    string translated = TranslateText(control.Text);
                    if (!string.Equals(translated, control.Text, StringComparison.Ordinal))
                    {
                        control.Text = translated;
                    }
                }
            }

            DataGridView grid = control as DataGridView;
            if (grid != null)
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (!string.IsNullOrEmpty(col.HeaderText))
                    {
                        col.HeaderText = TranslateText(col.HeaderText);
                    }

                    DataGridViewButtonColumn btnCol = col as DataGridViewButtonColumn;
                    if (btnCol != null && !string.IsNullOrEmpty(btnCol.Text))
                    {
                        btnCol.Text = TranslateText(btnCol.Text);
                    }
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyToControl(child);
            }
        }

        private static Dictionary<string, string> BuildReverseDictionary(Dictionary<string, string> source)
        {
            Dictionary<string, string> reverse = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in source)
            {
                if (!reverse.ContainsKey(pair.Value))
                {
                    reverse[pair.Value] = pair.Key;
                }
            }
            return reverse;
        }
    }
}
