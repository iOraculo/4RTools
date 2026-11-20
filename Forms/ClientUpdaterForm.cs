using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ClientUpdaterForm : Form
    {
        public ClientUpdaterForm()
        {
            InitializeComponent();
            LocalizationManager.ApplyToControlTree(this);
            ThemeManager.ApplyTheme(this, AppSettings.Load().DarkMode);
            this.Shown += ClientUpdaterForm_Shown;
        }

        private async void ClientUpdaterForm_Shown(object sender, EventArgs e)
        {
            await StartUpdateAsync();
        }

        private async Task StartUpdateAsync()
        {
            List<ClientDTO> clients = new List<ClientDTO>();

            try
            {
                // 1. Highest priority: local supported_servers.json overrides next to the executable.
                clients.AddRange(LocalServerManager.GetLocalClients());

                // 2. Remote supported_servers.json from the server's repository (allows live address updates).
                try
                {
                    string remoteServersRaw = await HttpClientProvider.GetStringAsync(AppConfig._4RClientsURL, TimeSpan.FromSeconds(5));
                    List<ClientDTO> remoteClients = JsonConvert.DeserializeObject<List<ClientDTO>>(remoteServersRaw);
                    if (remoteClients != null)
                    {
                        clients.AddRange(remoteClients);
                    }
                }
                catch (Exception remoteEx)
                {
                    Logger.Warn("ClientUpdaterForm", "Cannot load remote supported_servers.json; using local and embedded servers", remoteEx);
                }

                // 3. Always include embedded servers (including AureumRO) so standalone .exe works offline too.
                try
                {
                    List<ClientDTO> resourceClients = JsonConvert.DeserializeObject<List<ClientDTO>>(LoadResourceServerFile());
                    if (resourceClients != null)
                    {
                        clients.AddRange(resourceClients);
                    }
                }
                catch (Exception resEx)
                {
                    Logger.Error("ClientUpdaterForm", "Failed to load embedded supported_servers resource", resEx);
                }
            }
            finally
            {
                LoadServers(clients);
                if (!this.IsDisposed)
                {
                    this.Close();
                }
            }
        }

        public const string EXCLUSIVE_SERVER_NAME = "AureumRO";

        public static bool IsExclusiveServer(ClientDTO clientDTO)
        {
            return clientDTO != null
                && !string.IsNullOrWhiteSpace(clientDTO.name)
                && string.Equals(clientDTO.name.Trim(), EXCLUSIVE_SERVER_NAME, StringComparison.OrdinalIgnoreCase);
        }

        public static string LoadResourceServerFile()
        {
            return Resources._4RTools.ETCResource.supported_servers;
        }

        private void LoadServers(List<ClientDTO> clients)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.pbSupportedServer.Maximum = Math.Max(1, clients.Count);
            this.pbSupportedServer.Value = 0;

            foreach (ClientDTO clientDTO in clients)
            {
                try
                {
                    if (!IsExclusiveServer(clientDTO))
                    {
                        continue;
                    }

                    string normalizedName = clientDTO.name.Trim();
                    if (seen.Add(normalizedName))
                    {
                        ClientListSingleton.AddClient(new Client(clientDTO));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("ClientUpdaterForm", "Skipping invalid server entry: " + (clientDTO != null ? clientDTO.name : "null"), ex);
                }

                if (this.pbSupportedServer.Value < this.pbSupportedServer.Maximum)
                {
                    this.pbSupportedServer.Increment(1);
                }
            }
        }
    }
}
