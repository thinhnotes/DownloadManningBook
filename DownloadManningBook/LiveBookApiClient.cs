using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using DownloadManningBook.Model;
using Newtonsoft.Json;
using THttpWebRequest;

namespace DownloadManningBook
{
    public class LiveBookApiClient : TWebRequest
    {

        private string _bookName;
        private int _bookMeapVersion;
        public string Host;

        public LiveBookApiClient(string url, string proxyUrl = null) : base(new WebProxy(proxyUrl))
        {
            GetMeapVersion(url);
        }

        private string GetChapterContentUrl(string chapterName)
        {
            var url = $"https://livebook.manning.com/api/book/getBookElement?bookShortNameOrSlug={_bookName}&bookElementShortName={chapterName}&freeEbooksAreOpen=false&platform=browser-MacIntel";

            string result = Get(url);
            return JsonConvert.DeserializeObject<dynamic>(result)["bookElement"]["contentUrl"];
        }

        public List<Chapter> GetChapters()
        {
            string url = $"https://{Host}.cloudfront.net/{_bookName}/tocAndIndex.json";
            //string url = $"https://drek4537l1klr.cloudfront.net/{_bookName}/v-{_meapVersion}/tocAndIndex.json";
            string result = Get(url);
            var json = JsonConvert.DeserializeObject<Data>(result);
            return json.Toc.Parts.SelectMany(x => x.Chapters).ToList();
        }

        public string Unlock(string chapter, int paragraph)
        {
            string url = "https://livebook.manning.com/api/userBook/unlockBookElementContent";

            var content = Post(url, new KeyValuePair<string, string>("bookShortName", _bookName),
                new KeyValuePair<string, string>("bookElementShortName", chapter),
                new KeyValuePair<string, string>("paragraphIDs", paragraph.ToString()),
                new KeyValuePair<string, string>("meapVersion", _bookMeapVersion.ToString()),
                new KeyValuePair<string, string>("isSearch", "false"),
                new KeyValuePair<string, string>("isFreePreview", "true"),
                new KeyValuePair<string, string>("logTimings", "false"),
                new KeyValuePair<string, string>("previewDuration", "0.800000000000001"),
                new KeyValuePair<string, string>("timezone", "Asia/Shanghai"),
                new KeyValuePair<string, string>("platform", "browser-MacIntel"));

            var deserializeObject = JsonConvert.DeserializeObject<UnlockStage>(content);
            if (deserializeObject.Success == false)
                throw new Exception($"It not preview any more {deserializeObject.Reason}");
            return deserializeObject.UnlockedParagraphs.Count > 0 ? deserializeObject.UnlockedParagraphs[0].Content : null;
        }

        public bool GetPreviewDuration()
        {
            var content = Post("https://livebook.manning.com/freePreview/getFreePreviewStatus", $"bookShortName={_bookName}&platform=browser-Win32");
            var deserializeObject = JsonConvert.DeserializeObject<dynamic>(content);
            return deserializeObject.canFreelyPreview;
        }

        public void GetMeapVersion(string url)
        {
            var content = Get(url);
            var regex = new Regex("(?<host>[a-zA-Z0-9]+).cloudfront.net/(?<key>[a-zA-Z0-9]+)/Figures/cover.jpg");
            _bookName = regex.Match(content).Groups["key"].Value;
            var regexMeap = new Regex("MEAP V(?<key>[\\d]+)");
            Host = regex.Match(content).Groups["host"].Value;
            int.TryParse(regexMeap.Match(content).Groups["key"].Value, out int data);
            _bookMeapVersion = data;
        }

        public string GetChapter(string chapterName)
        {
            var chaperUrl = GetChapterContentUrl(chapterName);
            return Get(chaperUrl);
        }
    }
}
