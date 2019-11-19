using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace NetworkingLearning.HttpClientExample
{
    public class Downloader
    {
        public static string urlToDownload = "https://16bpp.net/";

        public static async Task DownloadWebPage()
        {
            Console.WriteLine("Hello World 2");

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage resp = await client.GetAsync(urlToDownload);
                Console.WriteLine("hmmmm");

                if (resp.IsSuccessStatusCode)
                {
                    Console.WriteLine("Got it...");

                    // byte[] data = await resp.Content.ReadAsByteArrayAsync();
                    string data = await resp.Content.ReadAsStringAsync();

                    Console.WriteLine(data);

                    Console.WriteLine("DONE!");
                }
            }
        }

        public Downloader()
        {
            Task dlTask = DownloadWebPage();

            Console.WriteLine("Hello World!");

            dlTask.GetAwaiter().GetResult();
        }
    }
}