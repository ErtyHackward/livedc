using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace LiveDc.Helpers
{
    public static class IpGeoBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void RequestAsync(IPAddress address, Action<IpGeoBaseResponse> callback)
        {
            var req = WebRequest.Create(string.Format("http://ipgeobase.ru:7020/geo?ip={0}", address));

            req.Timeout = 5000;
            req.BeginGetResponse(RequestFinished, Tuple.Create(req, callback));
        }

        private static void RequestFinished(IAsyncResult result)
        {
            var tuple = (Tuple<WebRequest, Action<IpGeoBaseResponse>>)result.AsyncState;

            var res = new IpGeoBaseResponse();

            try
            {
                var response = tuple.Item1.EndGetResponse(result);

                /*  <ip-answer>
                        <ip value="144.206.192.6">
                            <inetnum>144.206.0.0 - 144.206.255.255</inetnum>
                            <country>RU</country>
                            <city>Москва</city>
                            <region>Москва</region>
                            <district>Центральный федеральный округ</district>
                            <lat>55.755787</lat>
                            <lng>37.617634</lng>
                        </ip>
                    </ip-answer>
                 */

                var doc = new XmlDocument();

                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream, Encoding.GetEncoding(1251)))
                {
                    doc.LoadXml(sr.ReadToEnd());
                }

                var nodes = doc.GetElementsByTagName("ip");

                if (nodes.Count > 0)
                {
                    var node = nodes[0];

                    if (node.Attributes != null && node.Attributes["value"] != null)
                        res.IPAddress = node.Attributes["value"].InnerText;

                    if (node["inetnum"] != null)
                        res.IPRange = node["inetnum"].InnerText;

                    if (node["country"] != null)
                        res.Country = node["country"].InnerText;

                    if (node["city"] != null)
                        res.City = node["city"].InnerText;

                    if (node["region"] != null)
                        res.Region = node["region"].InnerText;

                    if (node["district"] != null)
                        res.District = node["district"].InnerText;

                    var ci = new CultureInfo("en-US");

                    if (node["lat"] != null)
                        res.Lat = float.Parse(node["lat"].InnerText, ci.NumberFormat);

                    if (node["lng"] != null)
                        res.Lng = float.Parse(node["lng"].InnerText, ci.NumberFormat);

                    res.Success = true;
                }
            }
            catch (Exception x)
            {
                Logger.Error("IpGeo request failed: {0}", x);
            }
            tuple.Item2(res);
        }
    }

    public struct IpGeoBaseResponse
    {
        /// <summary>
        /// Indicates if request is finished successfuly
        /// </summary>
        public bool Success { get; set; }

        public string IPAddress { get; set; }

        public string IPRange { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string Region { get; set; }

        public string District { get; set; }

        public float Lat { get; set; }

        public float Lng { get; set; }
    }
}
