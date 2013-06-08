using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LiveDc.Forms;
using LiveDc.Helpers;
using SharpDc;
using SharpDc.Connections;
using SharpDc.Events;

namespace LiveDc.Managers
{
    /// <summary>
    /// Allows to find hub addresses and share them between users
    /// </summary>
    public class HubManager
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DcEngine _engine;
        private readonly LiveClient _client;

        private List<HubConnection> _failedHubs = new List<HubConnection>();

        private List<string> _allHubs = new List<string>();

        private Settings Settings { get { return _client.Settings; } }

        public bool InitializationCompleted { get; private set; }
        
        public HubManager(DcEngine engine, LiveClient client)
        {
            _engine = engine;
            _client = client;

            _engine.Hubs.HubAdded += HubsHubAdded;
            _engine.Hubs.HubRemoved += HubsHubRemoved;
            _engine.ActiveStatusChanged += _engine_ActiveStatusChanged;

            if (!string.IsNullOrEmpty(Settings.Hubs))
            {
                var hubs = Settings.Hubs.Split(';');

                foreach (var hubAddress in hubs)
                {
                    AddHub(hubAddress);
                }
            }

            
        }

        void _engine_ActiveStatusChanged(object sender, EventArgs e)
        {
            InitializationCompleted = true;

            if (_client.Engine.Active)
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

            if (e.City != null)
            {
                LiveApi.GetHubsAsync(e.City, HubsListReceived);
                if (!string.IsNullOrEmpty(Settings.Hubs))
                {
                    LiveApi.PostHubsAsync(e.City, Settings.Hubs);
                }
            }
            else
                ShowHubEditDialog();
        }

        private void HubsListReceived(List<string> list)
        {
            if (list.Count > 0)
            {
                if (Settings.Hubs == null)
                    Settings.Hubs = "";
                else
                    Settings.Hubs += ";";

                Settings.Hubs += string.Join(";", list.Where(i => !Settings.Hubs.Contains(i)));
                Settings.Hubs = Settings.Hubs.Trim(';');
                Settings.Save();

                list.ForEach(AddHub);
            }
            else
            {
                ShowHubEditDialog();
            }
        }

        private void AddHub(string hubAddress)
        {
            if (_engine.Hubs.All().Any(h => h.Settings.HubAddress == hubAddress))
                return;

            logger.Info("Adding hub {0}", hubAddress);

            var hub = _engine.Hubs.Add(hubAddress, Settings.Nickname);
            hub.Settings.GetUsersList = false;
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

                if (_failedHubs.Count == _engine.Hubs.Count && !_client.Settings.DontOverrideHubs)
                {
                    InitializationCompleted = true;
                    _client.Settings.DontOverrideHubs = true;
                    ShowHubEditDialog();
                }
            }
        }

        private void ShowHubEditDialog()
        {
            _client.AddClickAction(() => new FrmHubList(_client).Show(),"Не удалось установить соединение ни с одним из хабов. Нажмите чтобы добавить хаб.", "hub_fail");
        }
    }
}
