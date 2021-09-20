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
        private string _host;
        private string _bookName;

        public Keeper(string url)
        {
            _documentLink = url;
            _httpClient = new HttpClient();
        }

        public async Task Init()
        {
            await GetMeapVersion();
            _client = new LiveBookApiClient(_bookName, _host);
        }

        private async Task GetMeapVersion()
        {
            var content = await _httpClient.GetStringAsync(_documentLink);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            var regex = new Regex("(?<host>[a-zA-Z0-9]+).cloudfront.net/(?<key>[a-zA-Z]+)/Figures/cover.jpg");
            _bookName = regex.Match(content).Groups["key"].Value;
            _host = regex.Match(content).Groups["host"].Value;
        }

        public async Task SaveEncrypted()
        {
            var chapters = _client.GetChapters();
            Console.WriteLine(chapters.Count);

            foreach (var chapter in chapters)
            {
                try
                {
                    var filename = $"{_tempLocation}/{_tempLocation}/" + chapter.ShortName + ".html";
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
            foreach (var chapter in _client.GetChapters())
            {
                await Unlock(chapter);
            }

            //Parallel.ForEach(_client.GetChapters(), new ParallelOptions { MaxDegreeOfParallelism = 3 },
            //async chapter =>
            //{
            //    await Unlock(chapter);
            //});
        }

        private async Task Unlock(Chapter chapter)
        {
            string contentPath = $"{_bookName}/{_tempLocation}/{chapter.ShortName}.html";
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
                    var outPath = $"{_bookName}/{_tempLocation}/{chapter.ShortName}-{paragraphId}.html";
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

            if (!Directory.Exists($"{_bookName}/{_saveLocation}"))
            {
                Directory.CreateDirectory($"{_bookName}/{_saveLocation}");
            }

            await File.WriteAllTextAsync($"{_bookName}/{this._saveLocation}/{chapter.ShortName}.html", htmlDocument.DocumentNode.OuterHtml.Replace("{{BOOK_ROOT_FOLDER}}", $"https://{_host}.cloudfront.net"));
        }
    }
}
