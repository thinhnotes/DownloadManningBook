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

            BeginUnlock:
            try
            {
                await keeper.Unlock();

            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("It not preview any more")) throw;
                Console.WriteLine("Please change the IP and press any key to continute;");
                Console.ReadKey();
                goto BeginUnlock;
            }
            Console.WriteLine("Download Completed!");
        }
    }
}
