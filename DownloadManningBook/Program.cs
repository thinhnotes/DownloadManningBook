using System;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadManningBook
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var bookUrl = Environment.GetEnvironmentVariable("BOOK_URL");

            if (bookUrl == null)
            {
                throw new Exception("Please input the bookName");
            }

            Console.WriteLine($"Init book with Url {bookUrl}");
            var keeper = new Keeper(bookUrl);

            await keeper.SaveEncrypted();

            await keeper.Unlock();

            bool isComplete;
            Thread.Sleep(120000);
            do
            {
                Console.WriteLine("Waiting for consumer unlock");
                isComplete = await keeper.CheckComplete();
                Thread.Sleep(10000);
            } while (!isComplete);


            Console.WriteLine("Beign FormatCalibre");

            await keeper.FormatCalibre();

            Console.WriteLine("Download Completed!");
        }
    }
}
