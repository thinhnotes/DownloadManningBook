using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DownloadManningBook.Model;
using HtmlAgilityPack;

namespace DownloadManningBook
{
    public class Keeper
    {
        private LiveBookApiClient _client;
        private readonly HttpClient _httpClient;
        private readonly string _tempLocation = "chapters";
        private readonly string _saveLocation = "out";
        private readonly string _documentLink;
        private readonly string _rootBookUrl = "https://dpzbhybb2pdcj.cloudfront.net";

        public Keeper(string url)
        {
            _documentLink = url;
            _httpClient = new HttpClient();
        }

        public async Task Init()
        {
            var meapVersion = await GetMeapVersion();
            _client = new LiveBookApiClient(meapVersion.Item1, meapVersion.Item2);
        }

        private async Task<(string, int)> GetMeapVersion()
        {
            var content = await _httpClient.GetStringAsync(_documentLink);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);
            var selectSingleNode = htmlDocument.DocumentNode.SelectSingleNode("//link[@rel='canonical']");
            var value = selectSingleNode.Attributes["href"].Value;

            var regex = new Regex("(?<key>[a-zA-Z]+)/Figures/cover.jpg");
            var name = regex.Match(content).Groups["key"].Value;
            return (name, int.Parse(value.Substring(value.Length - 1)));
        }

        private TocMeta GetTocAndIndex()
        {
            return _client.GetTocAndIndex();
        }

        public async Task SaveEncrypted()
        {
            TocMeta toc = GetTocAndIndex();
            Console.WriteLine(toc.Chapters.Count);

            foreach (var chapter in toc.Chapters)
            {
                try
                {
                    var filename = $"{_tempLocation}/" + chapter.ShortName + ".html";
                    if (File.Exists(filename)) continue;

                    var contentUrl = _client.GetChapterContentUrl(chapter.ShortName);
                    Console.WriteLine(chapter.ShortName + " " + contentUrl);

                    string content = await _httpClient.GetStringAsync(contentUrl);

                    if (!Directory.Exists(_tempLocation))
                    {
                        Directory.CreateDirectory(_tempLocation);
                    }
                    await File.WriteAllTextAsync(filename, content);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            Console.WriteLine("Saved content to files");
        }

        public async Task Unlock()
        {
            var tocAndIndex = _client.GetTocAndIndex();
            foreach (var chapter in tocAndIndex.Chapters)
            {
                await Unlock(chapter);
            }
        }

        private async Task Unlock(Chapter chapter)
        {
            string contentPath = $"{_tempLocation}/{chapter.ShortName}.html";
            string chapterContent = await File.ReadAllTextAsync(contentPath);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(chapterContent);
            var htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//*[contains(@class, 'scrambled')]");
            if (htmlNodeCollection != null)
            {
                var count = 0;
                foreach (var selectNode in htmlNodeCollection)
                {
                    count++;
                    var paragraphId = selectNode.Attributes["id"]?.Value;
                    if (string.IsNullOrWhiteSpace(paragraphId))
                    {
                        Console.WriteLine("Not found the paragraph Id ");
                        return;
                    }

                    Console.Title = $"{chapter.ShortName} - {paragraphId} - {count}/ {htmlNodeCollection.Count}";
                    string paragraph;
                    var outPath = $"{_tempLocation}/{chapter.ShortName}-{paragraphId}.html";
                    if (!File.Exists(outPath))
                    {
                        paragraph = _client.Unlock(chapter.ShortName, int.Parse(paragraphId));
                        await File.WriteAllTextAsync(outPath, paragraph);
                    }
                    else
                    {
                        paragraph = await File.ReadAllTextAsync(outPath);
                    }

                    try
                    {
                        selectNode.InnerHtml = paragraph;
                        //htmlDocument.DocumentNode.ReplaceChild(HtmlNode.CreateNode(paragraph), selectNode);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Cannot find element on paragram Id {paragraphId} with exception {e.Message}");   
                    }
                }
            }

            if (!Directory.Exists(_saveLocation))
            {
                Directory.CreateDirectory(_saveLocation);
            }

            await File.WriteAllTextAsync($"{this._saveLocation}/{chapter.ShortName}.html", htmlDocument.DocumentNode.OuterHtml.Replace("{{BOOK_ROOT_FOLDER}}", _rootBookUrl));
        }
    }
}
