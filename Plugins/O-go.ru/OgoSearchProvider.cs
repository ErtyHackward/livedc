using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using LiveDc.Providers;

namespace O_go.ru
{
    /// <summary>
    /// Provides search on the DcShara.ru site
    /// </summary>
    public class OgoProvider : IWebSearchProvider
    {
        public string ProviderName { get { return "o-go.ru"; } }
        public string TabTitle { get { return "o-go.ru"; } }

        public IAsyncResult SearchAsync(string query, Action<WebSearchResponse> callback)
        {
            if (query == null) throw new ArgumentNullException("query");
            if (callback == null) throw new ArgumentNullException("callback");

            WebRequest req = WebRequest.Create("http://client.o-go.ru/search.php?s=" + Uri.EscapeDataString(query));
            req.Method = "GET";
            req.Timeout = 5000;

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

                var data = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding(1251)).ReadToEnd();

                ea.Results = LoadXml(data).Select(r => new WebSearchResult { Name = r.Name, PosterUrl = r.Poster, ReleaseUrl = r.Link, Size = r.Size }).ToList();
                tuple.Item2(ea);
            }
            catch (Exception ex)
            {
                ea.Exception = ex;
                tuple.Item2(ea);
            }
        }

        /// <summary>
        /// Загружает результаты поиска из строки, содержащей xml данные
        /// </summary>
        /// <param name="xmlDocument">Строка, содержащая xml данные</param>
        public IEnumerable<OgoResult> LoadXml(string xmlDocument)
        {
            // поиск по сайту прошел
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlDocument);
            XmlNodeList list = doc.GetElementsByTagName("result");
            if (list != null)
            {
                foreach (XmlNode node in list)
                    yield return Add(node);
            }
        }

        protected OgoResult Add(XmlNode node)
        {
            OgoResult res = new OgoResult();
            try
            {
                res.Download = node["download"].InnerText;
                res.Link = node["link"].InnerText;
                res.Name = node["name"].InnerText;
                res.Rating = int.Parse(node["rating"].InnerText);
                res.Type = int.Parse(node["type"].InnerText);
                res.Size = long.Parse(node["size"].InnerText);
                if (node["avtor"] != null)
                    res.Releaser = node["avtor"].InnerText;
                if (node["sources"] != null)
                    res.Sources = int.Parse(node["sources"].InnerText);
                if (node["tags"] != null)
                    res.Tags = node["tags"].InnerText;
                res.Poster = node["poster"].InnerText;
            }
            catch (Exception ex)
            {
            }

            return res;
        }

    }

    [Serializable]
    public class OgoResult
    {
        [XmlElement("link")]
        public string Link { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("rating")]
        public int Rating { get; set; }
        [XmlElement("type")]
        public int Type { get; set; }
        [XmlElement("download")]
        public string Download { get; set; }
        [XmlElement("size")]
        public long Size { get; set; }
        [XmlElement("avtor")]
        public string Releaser { get; set; }
        [XmlElement("sources")]
        public int Sources { get; set; }
        [XmlElement("tags")]
        public string Tags { get; set; }
        [XmlElement("poster")]
        public string Poster { get; set; }
    }
}
