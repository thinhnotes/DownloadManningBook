using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadManningBook
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string bookUrl = args[0];
            if (bookUrl == null)
            {
                throw new Exception("Please input the bookName");
            }

            Console.WriteLine($"Init book with Url {bookUrl}");
            string proxy = null;
            if (args.Length == 3)
            {
                proxy = args[2];
            }
            var keeper = new Keeper(bookUrl, proxy);

            await keeper.SaveEncrypted();

            Console.WriteLine("Beign unlock to files");

            int replicaData = int.Parse(args[1]);
            await keeper.UnlockByShardingData(replicaData);

            var isComplete = await keeper.CheckComplete();
            if (isComplete)
            {
                Console.WriteLine("Beign FormatCalibre");

                await keeper.FormatCalibre();

                Console.WriteLine("Download Completed!");
            }
            else
            {
                Console.WriteLine("Sharding Data for this done!");
            }

        }
    }
}
