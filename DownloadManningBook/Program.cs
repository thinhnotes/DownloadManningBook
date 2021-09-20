using System;
using System.Threading.Tasks;

namespace DownloadManningBook
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string bookName = args[0];
            var keeper = new Keeper(bookName);
            await keeper.Init();
            await keeper.SaveEncrypted();
            await keeper.Unlock();
            Console.WriteLine("Download Completed!");
        }
    }
}
