using System;
using System.IO;
using System.Net;

namespace LiveDc.Helpers
{
    public static class LiveCheckIp
    {
        public static void CheckPortAsync(int tcpPort, Action<CheckIpResult> callback)
        {
            var req = WebRequest.Create(string.Format("http://livedc.april32.com/checkip.php?tcp={0}", tcpPort));

            req.Timeout = 5000;
            req.BeginGetResponse(RequestFinished, Tuple.Create(req, callback));
        }

        private static void RequestFinished(IAsyncResult result)
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
    }

    public class CheckIpResult
    {
        public string ExternalIpAddress { get; set; }
        public bool IsPortOpen { get; set; }
        public bool Failed { get; set; }
    }
}
