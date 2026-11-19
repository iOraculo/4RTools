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
                clients.AddRange(LocalServerManager.GetLocalClients());

                string remoteServersRaw = await HttpClientProvider.GetStringAsync(AppConfig._4RClientsURL, TimeSpan.FromSeconds(5));
                List<ClientDTO> remoteClients = JsonConvert.DeserializeObject<List<ClientDTO>>(remoteServersRaw);
                if (remoteClients != null)
                {
                    clients.AddRange(remoteClients);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ClientUpdaterForm", "Cannot load remote supported_servers.json, falling back to embedded resource", ex);
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

        private string LoadResourceServerFile()
        {
            return Resources._4RTools.ETCResource.supported_servers;
        }

        private void LoadServers(List<ClientDTO> clients)
        {
            this.pbSupportedServer.Maximum = Math.Max(1, clients.Count);
            this.pbSupportedServer.Value = 0;

            foreach (ClientDTO clientDTO in clients)
            {
                try
                {
                    ClientListSingleton.AddClient(new Client(clientDTO));
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
