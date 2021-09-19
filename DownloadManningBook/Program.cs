using System;
using System.Threading.Tasks;

namespace DownloadManningBook
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var keeper = new Keeper("https://livebook.manning.com/book/kubernetes-in-action");
            await keeper.Init();
            await keeper.SaveEncrypted();
            await keeper.Unlock();
            Console.WriteLine("Download Completed!");
        }
    }
}
