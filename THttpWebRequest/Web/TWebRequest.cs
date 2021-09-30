using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using THttpWebRequest.Utility;

namespace THttpWebRequest
{
    public enum RequestType
    {
        Normal,
        Json
    }

    public class TWebRequest
    {
        private string _userAgent;
        protected CookieCollection CookieCollection { get; set; }
        protected WebHeaderCollection WebHeaderCollection { get; set; }
        protected bool AutoRedirect { get; set; }
        protected string Location { get; set; }
        protected string Referer { get; set; }

        protected RequestType RequestType { get; set; }

        protected string UserAgent
        {
            get =>
                _userAgent ??
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36";
            set => _userAgent = value;
        }

        protected bool Gzip { get; set; }
        private Uri Uri { get; set; }
        public IWebProxy Proxy;

        protected TWebRequest(IWebProxy proxy = null)
        {
            Proxy = proxy;
            CookieCollection = new CookieCollection();
            WebHeaderCollection = new WebHeaderCollection();
        }

        protected TWebRequest(CookieCollection cookie)
        {
            CookieCollection = cookie;
        }

        protected TWebRequest(string fileName)
            : this()
        {
            string text = File.ReadAllText(fileName);
            var deserializeJsonAs = text.DeserializeJsonAs<IEnumerable<Dictionary<string, object>>>();
            var cookieCollection = new CookieCollection();
            foreach (var jsonA in deserializeJsonAs)
            {
                var cookie = new Cookie()
                {
                    Domain = (string)jsonA["domain"],
                    HttpOnly = (bool)jsonA["hostOnly"],
                    Name = (string)jsonA["name"],
                    Path = (string)jsonA["path"],
                    Value = (string)jsonA["value"],
                    Secure = (bool)jsonA["secure"],
                };
                cookieCollection.Add(cookie);
            }
            CookieCollection = cookieCollection;
        }

        private async Task<HttpWebResponse> GetResponseAsync(string url, string postData = null, string method = "GET")
        {
            Uri = new Uri(url);
            HttpWebRequest request = InitRequest();
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                request.Method = "POST";
                request.ContentType = RequestType == RequestType.Normal ? "application/x-www-form-urlencoded" : "application/json";
                if (postData != null)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = byteArray.Length;
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                }
                else
                    request.ContentLength = 0;
            }
            return (await request.GetResponseAsync() as HttpWebResponse);
        }
        
        private async Task<Stream> GetStreamAsync(string url, string postData = null, string method = "GET")
        {
            HttpWebResponse response;
            try
            {
                response = await GetResponseAsync(url, postData, method);
            }
            catch (WebException ex)
            {
                if(ex == null)
                {
                    throw;
                }
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Found)
                {
                    response = (HttpWebResponse)ex.Response;

                }
                else
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
                    {
                        Console.WriteLine(reader.ReadToEnd());
                    }

                    if ((int)ex.Status != 7)
                    {
                        throw;
                    }
                    else
                    {
                        response = (HttpWebResponse)ex.Response;
                    }
                }
            }

            if (AutoRedirect == false)
            {
                if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
                {
                    // Do something...
                    Location = response.Headers["Location"];
                }
            }
            CookieCollection.Add(response.Cookies);
            Stream responseStream = response.GetResponseStream();
            return responseStream;
        }

        private async Task<string> GetContent(string url, string postData = null, string method = "GET")
        {
            using (Stream stream = await GetStreamAsync(url, postData, method))
            {
                using (var sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        protected async Task<string> Get(string url, bool autoRedirect = false)
        {
            AutoRedirect = autoRedirect;
            return await GetContent(url);
        }

        protected async Task<string> Post(string url, string data, RequestType requestType = RequestType.Normal, bool autoRedirect = false)
        {
            AutoRedirect = autoRedirect;
            RequestType = requestType;
            return await GetContent(url, data, "POST");
        }

        public async Task<string> Post(string url, params KeyValuePair<string, string>[] data)
        {
            string dataOutput = data.Aggregate("", (current, keyValuePair) => current + $"{keyValuePair.Key}={keyValuePair.Value}&");
            return await Post(url, dataOutput);
        }

        private HttpWebRequest InitRequest()
        {
            var request = (HttpWebRequest)WebRequest.Create(Uri);
            SetInitRequest(request);
            if (request.CookieContainer == null)
                request.CookieContainer = new CookieContainer();
            request.CookieContainer = new CookieContainer();
            if (CookieCollection != null)
                request.CookieContainer.Add(CookieCollection);
            if (RequestType == RequestType.Json)
            {
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            if(Proxy != null)
            {
                request.Proxy = Proxy;
            }
            return request;
        }

        protected void SetInitRequest(HttpWebRequest request)
        {
            request.AllowAutoRedirect = AutoRedirect;
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en,vi;q=0.8,en-US;q=0.6");
            if (RequestType == RequestType.Json)
            {
                request.Accept = "application/json, text/plain, */*";
            }

            else
            {
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            }
            request.Referer = Referer;
            request.UserAgent = UserAgent;
            if (WebHeaderCollection != null)
            {
                request.Headers.Add(WebHeaderCollection);
            }
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate |
                                             DecompressionMethods.None;
        }
    }
}