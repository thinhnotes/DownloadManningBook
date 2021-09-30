using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DownloadManningBook.Model;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace DownloadManningBook
{
    public class Keeper
    {
        private LiveBookApiClient _client;

        private readonly RabbitmqHandler _rabbitmqHandler;
        private readonly string _tempLocation = "chapters";
        private readonly string _saveLocation = "out";

        private readonly string rabbitmqUrl = Environment.GetEnvironmentVariable("RABBITMQ_URL");

        public Keeper(string url)
        {
            Console.WriteLine($"Init rabbitmqUrl {rabbitmqUrl} ");
            _client = new LiveBookApiClient(url);
            _rabbitmqHandler = new RabbitmqHandler(rabbitmqUrl, "processing");
        }

        public async Task SaveEncrypted()
        {
            var chapters = await _client.GetChapters();
            Console.WriteLine($"This book has {chapters.Count} Chapters");

            foreach (var chapter in chapters)
            {
                var filename = $"{_tempLocation}/" + chapter.ShortName + ".html";
                if (File.Exists(filename)) continue;
                string content = await _client.GetChapter(chapter.ShortName);
                await SaveFileAsync(filename, content);
            }
            Console.WriteLine("Saved content to files");
        }

        async Task SaveFileAsync(string filePath, string content)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            await File.WriteAllTextAsync(filePath, content);
        }

        public async Task Unlock()
        {
            var chapters = await _client.GetChapters();
            foreach (var chapter in chapters)
            {
                await UnlockChapter(chapter, HandleUnlock);
            }

            Console.WriteLine($"Total Message is {_rabbitmqHandler.Count}");
        }

        public async Task<bool> CheckComplete()
        {
            var chapers = await _client.GetChapters();
            foreach (var chapter in chapers)
            {
                if (!await UnlockChapter(chapter, null))
                    return false;
            }
            return true;
        }

        public void HandleUnlock(string shortName, string paragraphId, string outPath)
        {
            var message = JsonConvert.SerializeObject(new QueueMessage
            {
                ShortName = shortName,
                ParagraphId = int.Parse(paragraphId),
                OutputPath = outPath
            });
            _rabbitmqHandler.Publish(message);
        }

        private async Task<bool> UnlockChapter(Chapter chapter, Action<string, string, string> action)
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
                        return false;
                    }
                    string paragraph;
                    var outPath = $"{_tempLocation}/{chapter.ShortName}-{paragraphId}.html";

                    if (!File.Exists(outPath))
                    {
                        if (action != null)
                        {
                            action.Invoke(chapter.ShortName, paragraphId, outPath);
                            continue;
                        }
                        else return false;
                    }
                    else
                    {
                        paragraph = await File.ReadAllTextAsync(outPath);
                    }

                    try
                    {
                        selectNode.InnerHtml = paragraph;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Cannot find element on paragram Id {paragraphId} with exception {e.Message}");
                    }
                }
            }
            await SaveFile(chapter, htmlDocument.DocumentNode.InnerHtml);
            return true;
        }

        private async Task SaveFile(Chapter chapter, string html)
        {
            await SaveFileAsync($"{this._saveLocation}/{chapter.ShortName}.html", html.Replace("{{BOOK_ROOT_FOLDER}}", $"https://{_client.Host}.cloudfront.net"));
        }

        public async Task FormatCalibre()
        {
            System.Collections.Generic.List<Chapter> chapters = await _client.GetChapters();
            foreach (var chapter in chapters)
            {
                string file = $"{this._saveLocation}/{chapter.ShortName}.html";
                var fileContent = await File.ReadAllTextAsync(file);

                //Replace Id can't start with the number
                Regex regex = new Regex(" id=\"(\\d+)\"");
                string result = regex.Replace(fileContent, $" id=\"{chapter.ShortName}$1\"");


                foreach (var item in chapters)
                {
                    var regexChaperNumber = new Regex("\\d+");
                    int chapterNumberInt = int.Parse(regexChaperNumber.Match(item.ShortName).Value);

                    //Replace chapter link html
                    string chaperUrl = $"/book/kubernetes-in-action/{item.ShortName}/ch{string.Format("{0:00}", chapterNumberInt)}\"";
                    result = result.Replace(chaperUrl, $"chapter-{chapterNumberInt}.html\"");
                }

                //Replace image link html
                Regex figureregex = new Regex($"/book/kubernetes-in-action/{chapter.ShortName}/([a-zA-Z0-9]+)");
                result = figureregex.Replace(result, $"#$1");

                //Replace wording link html
                result = result.Replace("!@%STYLE%@!", string.Empty);
                Regex cssregex = new Regex("{\"css\".+");
                result = cssregex.Replace(result, string.Empty);

                //Save file
                await SaveFileAsync($"{this._saveLocation}/{chapter.ShortName}.html", result);
            }
        }
    }
}
