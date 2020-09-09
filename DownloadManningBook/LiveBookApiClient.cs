using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DownloadManningBook.Model;
using Newtonsoft.Json;
using THttpWebRequest;

namespace DownloadManningBook
{
    public class LiveBookApiClient:TWebRequest
    {

        private readonly string _bookName;
        private readonly int _meapVersion;

        public LiveBookApiClient(string bookName, int meapVersion)
        {
            _bookName = bookName;
            _meapVersion = meapVersion;
        }

        public string GetChapterContentUrl(string chapterName)
        {
            var url = $"https://livebook.manning.com/api/book/getBookElement?bookShortNameOrSlug={_bookName}&bookElementShortName={chapterName}&freeEbooksAreOpen=false&platform=browser-MacIntel";

            string result = Get(url);
            return JsonConvert.DeserializeObject<dynamic>(result)["bookElement"]["contentUrl"];
        }

        public TocMeta GetTocAndIndex()
        {
            string url = $"https://dpzbhybb2pdcj.cloudfront.net/{_bookName}/v-{_meapVersion}/tocAndIndex.json";
            string result = Get(url);
            var json = JsonConvert.DeserializeObject<Data>(result);
            return json.Toc.Parts[0];
        }

        public string Unlock(string chapter, int paragraph)
        {
            string url = "https://livebook.manning.com/api/userBook/unlockBookElementContent";

            var content = Post(url, new KeyValuePair<string, string>("bookShortName", _bookName),
                new KeyValuePair<string, string>("bookElementShortName", chapter),
                new KeyValuePair<string, string>("paragraphIDs", paragraph + ""),
                new KeyValuePair<string, string>("meapVersion", _meapVersion + ""),
                new KeyValuePair<string, string>("isSearch", "false"),
                new KeyValuePair<string, string>("isFreePreview", "true"),
                new KeyValuePair<string, string>("logTimings", "false"),
                new KeyValuePair<string, string>("previewDuration", "0.800000000000001"),
                new KeyValuePair<string, string>("timezone", "Asia/Shanghai"),
                new KeyValuePair<string, string>("platform", "browser-MacIntel"));

            var deserializeObject = JsonConvert.DeserializeObject<UnlockStage>(content);
            if(deserializeObject.Success==false)
                throw new Exception($"It not preview any more {deserializeObject.Reason}");
            return deserializeObject.UnlockedParagraphs[0].Content;
        }

        public bool GetPreviewDuration()
        {
            var content = Post("https://livebook.manning.com/freePreview/getFreePreviewStatus", $"bookShortName={_bookName}&platform=browser-Win32");
            var deserializeObject = JsonConvert.DeserializeObject<dynamic>(content);
            return deserializeObject.canFreelyPreview;
        }
    }
}
