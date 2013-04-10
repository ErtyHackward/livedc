using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace LiveDc.Helpers
{
    public class LiveHubsApi
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
                        Encoding.UTF8.GetBytes(string.Format("city={0}&hubs={1}",
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
    }
}
