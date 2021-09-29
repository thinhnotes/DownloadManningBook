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

            string bookUrl = Environment.GetEnvironmentVariable("BOOK_URL");
            int.TryParse(Environment.GetEnvironmentVariable("REPLICA_COUNT"), out int replicaData);

            if (bookUrl == null)
            {
                throw new Exception("Please input the bookName");
            }

            if (replicaData < 1)
            {
                throw new Exception("Please input the REPLICA_COUNT");
            }

            Console.WriteLine($"Init book with Url {bookUrl}");
            string proxy = Environment.GetEnvironmentVariable("PROXY");

            var keeper = new Keeper(bookUrl, proxy);

            await keeper.SaveEncrypted();

            Console.WriteLine("Beign unlock to files");

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
