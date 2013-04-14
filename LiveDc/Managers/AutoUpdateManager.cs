using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using LiveDc.Helpers;
using LiveDc.Notify;

namespace LiveDc.Managers
{
    public class AutoUpdateManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LiveClient _client;
        private string _updateTempFile;
        private string _readyUpdateFile;

        public AutoUpdateManager(LiveClient client)
        {
            _client = client;
        }

        public void CheckUpdate()
        {
            Logger.Info("Checking for update...");
            LiveApi.GetLastProgramVersion(VersionReceived);
        }

        private void VersionReceived(VersionResult result)
        {
            if (result.Failed)
                return;
            
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                var previousDistributive = Path.Combine(Path.GetTempPath(), string.Format("livedc_update_{0}.exe", currentVersion));

                if (File.Exists(previousDistributive))
                {
                    File.Delete(previousDistributive);
                }

                if (result.Version > currentVersion)
                {
                    var wc = new WebClient();

                    _readyUpdateFile = Path.Combine(Path.GetTempPath(), string.Format("livedc_update_{0}.exe", result.Version));

                    if (File.Exists(_readyUpdateFile))
                    {
                        RequestUpdate();
                        return;
                    }

                    _updateTempFile = Path.GetTempFileName();
                    wc.DownloadFile(result.DownloadUri, _updateTempFile);

                    File.Move(_updateTempFile, _readyUpdateFile);

                    RequestUpdate();
                }
                else
                {
                    Logger.Info("No need to update the client");
                }
            }
            catch (Exception x)
            {
                Logger.Error("Update error: {0}", x.Message);
            }
        }

        private void RequestUpdate()
        {
            _client.AddClickAction(() => {
                if (MessageBox.Show("Установить обновление сейчас?", "LiveDC - Обновление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (WindowsHelper.IsWinVistaOrHigher)
                    {
                        NativeMethods.RegisterApplicationRestart(null, 0);
                    }

                    WindowsHelper.RunElevated(_readyUpdateFile, "/VERYSILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");

                    if (!WindowsHelper.IsWinVistaOrHigher)
                        Application.Exit();
                }
            }, "Обновление клиента загружено и готово к установке. Нажмита сюда, чтобы обновить программу.");
        }
    }
}
