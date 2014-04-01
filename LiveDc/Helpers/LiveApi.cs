using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace LiveDc.Helpers
{
    public class LiveApi
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string PortCheckUri { get; set; }

        static LiveApi()
        {
            PortCheckUri = "http://livedc.april32.com/checkip.php";
        }

        public static void GetHubsAsync(string city, Action<List<string>> callback)
        {
            var req = WebRequest.Create(string.Format("http://livedc.april32.com/getHubs.php?city={0}", Uri.EscapeDataString(city)));

            req.Timeout = 5000;
            req.BeginGetResponse(RequestFinished, Tuple.Create(req, callback));
        }

        private static void RequestFinished(IAsyncResult result)
        {
            var tuple = (Tuple<WebRequest, Action<List<string>>>)result.AsyncState;

            var res = new List<string>();

            try
            {
                var response = tuple.Item1.EndGetResponse(result);

                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream))
                {
                    Logger.Info("Received response from livedc server");
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Logger.Info("Received hub: {0}", line);
                        res.Add(line);
                    }
                }
            }
            catch (Exception x)
            {
                Logger.Error("Unable to get hubs from live service {0}", x);
            }
            tuple.Item2(res);
        }
        
        /// <summary>
        /// Posts hubs to livedc server
        /// </summary>
        /// <param name="city">City of the user</param>
        /// <param name="hubs">hubs string separated by ;</param>
        public static void PostHubsAsync(string city, string hubs)
        {
            new ThreadStart(delegate
            {
                try
                {
                    var postBytes =
                        Encoding.UTF8.GetBytes(string.Format("v=2&city={0}&hubs={1}",
                                                             Uri.EscapeDataString(city),
                                                             Uri.EscapeDataString(hubs)));

                    var request =
                        WebRequest.Create("http://livedc.april32.com/saveHubs.php");
                    request.Method = "POST";

                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = postBytes.Length;

                    using (var requestStream = request.GetRequestStream())
                        requestStream.Write(postBytes, 0, postBytes.Length);

                    var response = request.GetResponse();
                }
                catch (Exception x)
                {
                    Logger.Error("Unable to post hubs to the server: {0}", x.Message);
                }
            }).BeginInvoke(null, null);
        }
        
        public static void CheckPortAsync(int tcpPort, Action<CheckIpResult> callback)
        {
            var req = WebRequest.Create(string.Format("{0}?tcp={1}", PortCheckUri, tcpPort));

            req.Timeout = 5000;
            req.BeginGetResponse(RequestPortFinished, Tuple.Create(req, callback));
        }

        private static void RequestPortFinished(IAsyncResult result)
        {
            var tuple = (Tuple<WebRequest, Action<CheckIpResult>>)result.AsyncState;

            var res = new CheckIpResult();

            try
            {
                var response = tuple.Item1.EndGetResponse(result);

                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream))
                {
                    res.ExternalIpAddress = sr.ReadLine();
                    res.IsPortOpen = Boolean.Parse(sr.ReadLine());
                }
            }
            catch (Exception x)
            {
                res.Failed = true;
            }
            tuple.Item2(res);
        }

        public static void GetLastProgramVersion(Action<VersionResult> callback)
        {
            var req = WebRequest.Create(string.Format("http://livedc.april32.com/version.idx"));

            req.Timeout = 5000;
            req.BeginGetResponse(VersionFinished, Tuple.Create(req, callback));
        }

        private static void VersionFinished(IAsyncResult result)
        {
            var tuple = (Tuple<WebRequest, Action<VersionResult>>)result.AsyncState;

            var res = new VersionResult();

            try
            {
                var response = tuple.Item1.EndGetResponse(result);

                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream))
                {
                    res.Version = Version.Parse(sr.ReadLine());
                    res.DownloadUri = sr.ReadLine();
                }
            }
            catch (Exception x)
            {
                res.Failed = true;
                Logger.Error("Exception on updateinfo request {0}", x.Message);
            }
            tuple.Item2(res);
        }
    }

    public class VersionResult
    {
        public bool Failed;
        public Version Version;
        public string DownloadUri;
    }

    public class CheckIpResult
    {
        public string ExternalIpAddress { get; set; }
        public bool IsPortOpen { get; set; }
        public bool Failed { get; set; }
    }
}
