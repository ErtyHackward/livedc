using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using LiveDc.Providers;

namespace DcShara.ru
{
    /// <summary>
    /// Provides search on the DcShara.ru site
    /// </summary>
    public class DcSharaProvider : IWebSearchProvider
    {
        public string ProviderName { get { return "DcShara.ru"; } }
        public string TabTitle { get { return "DcShara.ru"; } }

        public IAsyncResult SearchAsync(string query, Action<WebSearchResponse> callback)
        {
            if (query == null) throw new ArgumentNullException("query");
            if (callback == null) throw new ArgumentNullException("callback");

            WebRequest req = WebRequest.Create("http://dcshara.ru");

            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.Timeout = 5000;
            var reqStream = req.GetRequestStream();

            using (var sw = new StreamWriter(reqStream))
            {
                var pars = string.Format("do=search&subaction=search&story={0}", Uri.EscapeDataString(query));
                sw.Write(pars);
            }

            return req.BeginGetResponse(Response, Tuple.Create(req, callback));
        }

        private void Response(IAsyncResult result)
        {
            var tuple = (Tuple<WebRequest, Action<WebSearchResponse>>)result.AsyncState;
            var ea = new WebSearchResponse { Provider = this };

            try
            {
                var req = tuple.Item1;
                var resp = req.EndGetResponse(result);

                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    var data = reader.ReadToEnd();
                    ea.Results = ParseResults(data).ToList();
                }

                tuple.Item2(ea);
            }
            catch (Exception ex)
            {
                ea.Exception = ex;
                tuple.Item2(ea);
            }
        }

        private static IEnumerable<WebSearchResult> ParseResults(string data)
        {
            /*
                  <div class="prew-film">
                <div style="padding: 0px 0px 0px 30px;">
                <div class="prew-film-content">
                <div class="box4"><a href="http://dcshara.ru/uzhasy/14831118-pod-kupolom-under-the-dome-1-sezon.html" class="tip-bottom item" ><img width="155" height="228" src="http://dcshara.ru/uploads/posts/2013-06/1372606952_1372224699_kupol.jpg" alt="Под куполом - Under the Dome (1 сезон)" />
                <span class="tt opacity"><h2>Под куполом - Under the Dome (1 сезон)</h2></span></a></div>
                </div>
                <div class="prew-film-title">
                <div style="clear:both; height:10px;"></div>
                <div align="center" class="date" style="height:30px; overflow:hidden;"><a href="http://dcshara.ru/uzhasy/">ужасы</a>, <a href="http://dcshara.ru/fantastika/">фантастика</a>, <a href="http://dcshara.ru/serial/">сериал</a></div>
                <div class="bt2"><a href="http://dcshara.ru/uzhasy/14831118-pod-kupolom-under-the-dome-1-sezon.html">СКАЧАТЬ</a></div>
                <div><a href="#" title=""><img width="70" height="18" src="/templates/Cinema/images/smt.jpg" alt="" /></a></div>
                </div>
                </div>
                </div>
            */

            int startIndex = 0;

            while ((startIndex = data.IndexOf("class=\"box4\"", startIndex)) != -1)
            {
                // class="box4"><a href="http://dcshara.ru/uzhasy/14831118-pod-kupolom-under-the-dome-1-sezon.html" class="tip-bottom item" ><img width="155" height="228" src="http://dcshara.ru/uploads/posts/2013-06/1372606952_1372224699_kupol.jpg" alt="Под куполом - Under the Dome (1 сезон)" 

                WebSearchResult res = new WebSearchResult();

                var linkStartInd = data.IndexOf("http://", startIndex);
                var linkEndInd = data.IndexOf("\" class", linkStartInd);

                res.ReleaseUrl = data.Substring(linkStartInd, linkEndInd - linkStartInd);

                var posterStartInd = data.IndexOf("http://", linkEndInd);
                var posterEndInd = data.IndexOf("\" alt=", posterStartInd);

                res.PosterUrl = data.Substring(posterStartInd, posterEndInd - posterStartInd);

                var nameStartInd = data.IndexOf("alt=\"", posterEndInd) + 5;
                var nameEndInd = data.IndexOf("\" />", nameStartInd);

                res.Name = data.Substring(nameStartInd, nameEndInd - nameStartInd);

                yield return res;
                startIndex++;
            }
        }
    }
}



