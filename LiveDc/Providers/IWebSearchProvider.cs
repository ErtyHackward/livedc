using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using SharpDc.Interfaces;

namespace LiveDc.Providers
{
    public interface IWebSearchProvider
    {
        string ProviderName { get; }
        string TabTitle { get; }

        IAsyncResult SearchAsync(string query, Action<WebSearchResponse> callback);
    }

    public class WebSearchResponse : EventArgs
    {
        public IWebSearchProvider Provider { get; set; }
        public List<WebSearchResult> Results { get; set; }
        public Exception Exception { get; set; }
    }

    public class WebSearchResult : ISearchResult
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Image _poster;
        private bool _requestSent;

        public string ReleaseUrl { get; set; }
        public string PosterUrl { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }

        public Image Poster
        {
            get
            {
                if (_poster != null)
                    return _poster;

                lock (this)
                {
                    if (!_requestSent)
                    {
                        DownloadPosterAsync(PosterUrl);
                    }
                }

                return null;
            }
        }

        public event EventHandler PosterReceived;

        protected virtual void OnPosterReceived()
        {
            var handler = PosterReceived;
            if (handler != null) handler(this, EventArgs.Empty);
        }
        
        public object Clone()
        {
            throw new NotSupportedException();
        }

        public void DownloadPosterAsync(string url)
        {
            var myRequest = (HttpWebRequest)WebRequest.Create(url);
            myRequest.Method = "GET";
            myRequest.BeginGetResponse(ImageReceived, myRequest);
        }

        private void ImageReceived(IAsyncResult ar)
        {
            try
            {
                var tuple = (HttpWebRequest)ar.AsyncState;
                var myResponse = (HttpWebResponse)tuple.EndGetResponse(ar);
                var bmp = new Bitmap(myResponse.GetResponseStream());
                myResponse.Close();
                _poster = bmp;
                OnPosterReceived();
            }
            catch (Exception ex)
            {
                Logger.Error("Unable to load dcShara image {0}", ex);
            }
        }

    }
}
