using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LiveDc.Forms;
using LiveDc.Helpers;
using LiveDc.Providers;
using SharpDc.Connections;
using SharpDc.Events;

namespace LiveDc.Managers
{
    /// <summary>
    /// Allows to find hub addresses and share them between users
    /// </summary>
    public class DcHubManager
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DcProvider _provider;
        private readonly LiveClient _client;

        private List<HubConnection> _failedHubs = new List<HubConnection>();
        private List<string> _allHubs = new List<string>();

        private Settings Settings { get { return _client.Settings; } }

        public bool InitializationCompleted { get; private set; }
        
        public DcHubManager(DcProvider provider, LiveClient client)
        {
            _provider = provider;
            _client = client;

            _provider.Engine.Hubs.HubAdded += HubsHubAdded;
            _provider.Engine.Hubs.HubRemoved += HubsHubRemoved;
            _provider.Engine.ActiveStatusChanged += EngineActiveStatusChanged;

            if (!string.IsNullOrEmpty(Settings.Hubs))
            {
                var hubs = Settings.Hubs.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var hubAddress in hubs.Take(5))
                {
                    _allHubs.Add(hubAddress);
                    AddHub(hubAddress);
                }
            }
        }

        void EngineActiveStatusChanged(object sender, EventArgs e)
        {
            InitializationCompleted = true;

            if (_provider.Engine.Active)
            {
                _client.RemoveActionById("hub_fail");
            }
        }

        public void FindHubs(IPAddress externalIp)
        {
            if (FlyLinkHelper.IsInstalled)
            {
                var hubs = FlyLinkHelper.ReadHubs();

                for (int i = 0; i < hubs.Count; i++)
                {
                    if (hubs[i].StartsWith("dchub://"))
                        hubs[i] = hubs[i].Remove(0, 8);
                }

                logger.Info("Found FlyLink hubs: "+ string.Join(", ", hubs));

                _allHubs.AddRange(hubs);
            }

            if (StrongDcHelper.IsInstalled)
            {
                var hubs = StrongDcHelper.ReadHubs();

                for (int i = 0; i < hubs.Count; i++)
                {
                    if (hubs[i].StartsWith("dchub://"))
                        hubs[i] = hubs[i].Remove(0, 8);
                }

                logger.Info("Found StrongDC hubs: " + string.Join(", ", hubs));

                _allHubs.AddRange(hubs);
            }

            IpGeoBase.RequestAsync(externalIp, CityReceived);
        }
        
        private void CityReceived(IpGeoBaseResponse e)
        {
#if DEBUG
            e.City = "Кемерово";
#endif

            logger.Info("It seems we are in {0}", e.City);

            Settings.City = e.City;

            if (e.City != null && Settings.UpdateHubs)
            {
                LiveApi.GetHubsAsync(e.City, HubsListReceived);
            }
            else if (_allHubs.Count == 0 && _provider.Engine.Hubs.Count == 0)
            {
                ShowHubEditDialog("Не удалось найти хабы. Нажмите, чтобы добавить хаб.");
            }
            else
            {
                VerifyHubs();
            }
        }

        /// <summary>
        /// Check that we do not have duplicate hubs and submit them to the server
        /// </summary>
        private void VerifyHubs()
        {
            // step 1: normalize all hub addresses
            for (int i = 0; i < _allHubs.Count; i++)
            {
                _allHubs[i] = NormalizeHubAddress(_allHubs[i]);
            }

            // step 2: remove obvius duplicates
            _allHubs = _allHubs.Distinct().ToList();

            // step 3: find ip addresses of domain names to exclude situation when we have 2 instance of the same hub (ip and dns)
            var ipList = new List<string>(_allHubs);

            for (int i = 0; i < ipList.Count; i++)
            {
                if (!isIp(ipList[i]))
                {
                    string port;
                    var ip = extractIp(ipList[i], out port);
                    try
                    {
                        ipList[i] = Dns.GetHostEntry(ip).AddressList[0] + (port == null ? "" : ":" + port);
                    }
                    catch (Exception x)
                    {
                        logger.Error("unable to resolve hub {0} : {1}", ipList[i], x.Message);
                        ipList[i] = null;
                    }
                }
            }

            // remove hubs with invalid dns responses
            for (int i = _allHubs.Count - 1; i >= 0; i--)
            {
                if (ipList[i] == null)
                {
                    _allHubs.RemoveAt(i);
                    ipList.RemoveAt(i);
                }
            }

            // exclude duplicates
            var list = _allHubs.Select((h, i) => new { dns = h, ip = ipList[i]} ).GroupBy(t => t.ip).Select(t => t.First().dns).ToList();
            
            Settings.Hubs = string.Join(";", list);
            Settings.LastHubCheck = DateTime.Now;
            Settings.Save();

            foreach (var hub in list)
            {
                AddHub(hub);
            }

            if (!string.IsNullOrEmpty(Settings.Hubs) && !string.IsNullOrEmpty(Settings.City))
            {
                LiveApi.PostHubsAsync(Settings.City, Settings.Hubs);
            }
        }

        private bool isIp(string input)
        {
            if (input.StartsWith("dchub://"))
                input = input.Remove(0, 8);
            IPAddress addr;
            return IPAddress.TryParse(input, out addr);
        }

        private string extractIp(string input, out string port)
        {
            if (input.StartsWith("dchub://"))
                input = input.Remove(0, 8);
            port = null;

            var index = input.IndexOf(":");
            if (index != -1)
            {
                port = input.Substring(index + 1);
                input = input.Substring(0, index);
            }

            return input;
        }

        private void HubsListReceived(List<string> list)
        {
            logger.Info("Received hubs from server: "+ string.Join(", ", list));

            if (list.Count > 0)
            {
                _allHubs.AddRange(list);
            }

            if (_allHubs.Count == 0)
            {
                ShowHubEditDialog();
            }
            else
            {
                VerifyHubs();
            }
        }

        private void AddHub(string hubAddress)
        {
            if (_provider.Engine.Hubs.All().Any(h => h.Settings.HubAddress == hubAddress))
                return;

            logger.Info("Adding hub {0}", hubAddress);

            try
            {
                var hub = _provider.Engine.Hubs.Add(hubAddress, Settings.Nickname);
                hub.Settings.GetUsersList = false;
            }
            catch (Exception x)
            {
                logger.Error("Unable to add hub: {0} {1}", hubAddress , x.Message);
            }
        }

        void HubsHubRemoved(object sender, HubsChangedEventArgs e)
        {
            e.Hub.ConnectionStatusChanged -= HubOnConnectionStatusChanged;
            e.Hub.ActiveStatusChanged -= HubActiveStatusChanged;
        }

        void HubsHubAdded(object sender, HubsChangedEventArgs e)
        {
            e.Hub.ConnectionStatusChanged += HubOnConnectionStatusChanged;
            e.Hub.ActiveStatusChanged += HubActiveStatusChanged;
            logger.Info("Hub added {0}", e.Hub.Settings.HubAddress);
        }

        void HubActiveStatusChanged(object sender, EventArgs e)
        {
            var hub = (HubConnection)sender;
            if (hub.Active)
            {
                lock (_failedHubs)
                {
                    _failedHubs.Remove(hub);
                }
            }
        }

        private void HubOnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            var hub = (HubConnection)sender;

            logger.Info("Hub connection status {0} {1} {2}", hub.Settings.HubAddress, e.Status, e.Exception);

            if (e.Status == ConnectionStatus.Disconnected)
            {
                
                lock (_failedHubs)
                {
                    if (!_failedHubs.Contains(hub))
                        _failedHubs.Add(hub);
                }

                if (_failedHubs.Count == _provider.Engine.Hubs.Count && !_client.Settings.DontOverrideHubs)
                {
                    InitializationCompleted = true;
                    _client.Settings.DontOverrideHubs = true;
                    ShowHubEditDialog();
                }
            }
        }

        private void ShowHubEditDialog(string message = null)
        {
            _client.AddClickAction(() => new FrmHubList(_client, _provider).Show(), message ?? "Не удалось установить соединение ни с одним из хабов. Нажмите чтобы добавить хаб.", "hub_fail");
        }

        public static string NormalizeHubAddress(string address)
        {
            var result = address.ToLower();

            if (result.StartsWith("dchub://"))
                result = result.Remove(0, 8);

            if (result.EndsWith(":411"))
            {
                result = result.Remove(result.Length - 4);
            }

            return result;
        }
    }
}
