using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using LiveDc.Providers;
using NLog;
using SharpDc.Connections;
using SharpDc.Structs;
using SharpDc.WebServer;

namespace LiveDc
{
    /// <summary>
    /// Allows web based management of the client
    /// </summary>
    public class LiveDcWeb
    {
        private readonly IEnumerable<IFsProvider> _providers;
        private readonly WebServer _webServer;
        
        public LiveDcWeb(int httpPort, IEnumerable<IFsProvider> providers)
        {
            _providers = providers;
            _webServer = new WebServer { Port = httpPort };
            _webServer.Start();
            _webServer.Request += _webServer_Request;
        }

        void _webServer_Request(object sender, WebServerRequestEventArgs e)
        {
            //magnet:?xt=urn:tree:tiger:ITLSJ3MYQ2WKU5XHBIS4XDFK24QPXMJ3XXEN6PA&xl=2173134776&dn=Motylki.2013.E01.1080p.WEB-DL.-CasStudio.mkv
            //?action=prepare&magnet=magnet%3A%3Fxt%3Durn%3Atree%3Atiger%3AITLSJ3MYQ2WKU5XHBIS4XDFK24QPXMJ3XXEN6PA%26xl%3D2173134776%26dn%3DMotylki.2013.E01.1080p.WEB-DL.-CasStudio.mkv

            var qIndex = e.Request.URL.IndexOf('?');

            if (qIndex == -1)
                return;

            var paramsStr = e.Request.URL.Substring(qIndex+1);
            var pars = paramsStr.Split('&');


            var paramsList = new Dictionary<string,string>(); 
            foreach (var p in pars)
            {
                var eqInd = p.IndexOf('=');
                string key,value = null;

                if (eqInd == -1)
                {
                    key = p;
                }
                else
                {
                    key = p.Substring(0,eqInd);
                    value = p.Substring(eqInd+1);
                }

                paramsList.Add(key, value);
            }

            string action;
            if (paramsList.TryGetValue("action", out action))
            {
                string magnetStr;

                Magnet magnet = new Magnet();
                IFsProvider provider = null;
                if (paramsList.TryGetValue("magnet", out magnetStr))
                {
                    magnet = new Magnet(magnetStr);
                    provider = _providers.FirstOrDefault(p => p.CanHandle(magnet));
                }
                
                switch (action)
                {
                    case "prepare":
                        {
                            if (provider == null)
                            {
                                return;
                            }
                            
                        }
                        break;
                    case "file":
                        {
                            if (provider == null)
                            {
                                return;
                            }

                            var stream = provider.GetStream(magnet);

                            long start = 0;
                            long end = magnet.Size-1;

                            string range;
                            if (e.Request.Headers.TryGetValue("Content-Range", out range))
                            {
                                if (!ParseRange(range, out start, out end))
                                    return;
                                if (end == -1)
                                    end = magnet.Size -1;
                            }

                            e.Response.ContentLength64 = magnet.Size;
                            e.Response.Headers.Add("Content-Range", string.Format("bytes {0}-{1}/{2}", start, end, magnet.Size));
                            
                            stream.Seek(start, SeekOrigin.Begin);
                            var bytes = new byte[1024 * 64];
                            while (stream.Position != end + 1)
                            {
                                int bytesToRead;
                                if (end + 1 - stream.Position > bytes.Length)
                                {
                                    bytesToRead = bytes.Length;
                                }
                                else
                                {
                                    bytesToRead = (int)(end + 1 - stream.Position);
                                }

                                if (stream.Read(bytes,0, bytesToRead) != bytesToRead)
                                    return;

                                e.Response.Write(bytes, 0, bytesToRead);
                            }
                            e.Response.Close();
                        }

                        break;
                    default:
                        break;
                }




            }
        }

        private bool ParseRange(string raw, out long start, out long end)
        {
            start = 0;
            end = 0;
            // bytes 0-499

            if (raw.StartsWith("bytes"))
                raw = raw.Remove(0, 5);

            var spl = raw.Split('-');
            if (spl.Length != 2)
                return false;

            long.TryParse(spl[0], out start);
            if (!long.TryParse(spl[1], out end))
                end = -1;

            return true;
        }
    }
}
