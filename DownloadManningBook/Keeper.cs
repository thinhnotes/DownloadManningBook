using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DownloadManningBook.Model;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace DownloadManningBook
{
    public class Keeper
    {
        private LiveBookApiClient _client;

        private readonly RabbitmqHandler _rabbitmqHandler;
        private readonly string _tempLocation = "chapters";
        private readonly string _saveLocation = "out";


        public Keeper(string url, string proxy = null)
        {
            _client = new LiveBookApiClient(url, proxy);
            //use for queue
            //_rabbitmqHandler = new RabbitmqHandler("localhost", "processing");
            //_rabbitmqHandler.Consumer(_client);
        }

        public async Task SaveEncrypted()
        {
            var chapters = _client.GetChapters();
            Console.WriteLine($"This book has {chapters.Count} Chapters");

            foreach (var chapter in chapters)
            {
                var filename = $"{_tempLocation}/" + chapter.ShortName + ".html";
                if (File.Exists(filename)) continue;
                string content = _client.GetChapter(chapter.ShortName);
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

        //public async Task Unlock()
        //{
        //    var chapters = _client.GetChapters().ToArray();
        //    foreach (var chapter in chapters)
        //    {
        //        await UnlockChapter(chapter);
        //    }
        //}

        public async Task UnlockByShardingData(int nunberCost)
        {
            Console.WriteLine($"Number of sharding {nunberCost}");

            string _fileLine = "out/machine.txt";
            if (!Directory.Exists("out"))
                Directory.CreateDirectory("out");
            File.AppendAllText($"out/machine.txt", Environment.MachineName + Environment.NewLine);

            var machines = File.ReadAllLines(_fileLine);

            var machineIndex = Array.FindIndex(machines, w => w == Environment.MachineName) % nunberCost;

            var chapers = _client.GetChapters().ToArray();
            for (int i = machineIndex; i < chapers.Length; i = i + nunberCost)
            {
                Console.WriteLine($"Chapter index on {i}");
                await UnlockChapter(chapers[i], HandleUnlock);
            }
        }

        public async Task<bool> CheckComplete() 
        {
            var chapers = _client.GetChapters().ToArray();
            foreach (var chapter in chapers)
            {
                if(!await UnlockChapter(chapter, HandleChecking))
                    return false;
            }
            return true;
        }

        public async Task<string> HandleChecking(string shortName, string paragraphId, string outPath)
        {
            return null;
        }

        public async Task<string> HandleUnlock(string shortName, string paragraphId, string outPath)
        {
            Console.WriteLine($"{Environment.MachineName} - {shortName} - {paragraphId} ");
            //use message queue
            if (_rabbitmqHandler != null)
            {
                var message = JsonConvert.SerializeObject(new QueueMessage
                {
                    ShortName = shortName,
                    ParagraphId = int.Parse(paragraphId),
                    OutputPath = outPath
                });
                _rabbitmqHandler.Publish(message);
            }

            //use the single thread
            var paragraph = _client.Unlock(shortName, int.Parse(paragraphId));
            await SaveFileAsync(outPath, paragraph);
            return paragraph;
        }



        private async Task<bool> UnlockChapter(Chapter chapter, Func<string, string, string, Task<string>> func)
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
                        paragraph = await func(chapter.ShortName, paragraphId, outPath);
                        if (paragraph == null)
                        {
                            return false;
                        }
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
            await SaveFile(chapter, htmlDocument.DocumentNode.InnerHtml);
            return true;
        }

        private async Task SaveFile(Chapter chapter, string html)
        {
            await SaveFileAsync($"{this._saveLocation}/{chapter.ShortName}.html", html.Replace("{{BOOK_ROOT_FOLDER}}", $"https://{_client.Host}.cloudfront.net"));
        }

        public async Task FormatCalibre()
        {
            System.Collections.Generic.List<Chapter> chapters = _client.GetChapters();
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
