﻿using System;
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
            do
            {
                Console.WriteLine("Waiting for consumer unlock");
                Thread.Sleep(10000);
                isComplete = await keeper.CheckComplete();
            } while (!isComplete);


            Console.WriteLine("Beign FormatCalibre");

            await keeper.FormatCalibre();

            Console.WriteLine("Download Completed!");
        }
    }
}
