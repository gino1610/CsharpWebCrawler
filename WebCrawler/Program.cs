using System;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {

            // var topLevelUrl = @"https://www.cnn.com";
            // var topLevelUrl = @"https://www.google.com";
            var topLevelUrl = @"https://www.redhat.com";
            var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var reportFile = @"D:\Temp\GoWebCrawlerReport_" + date + ".txt";
            Crawler crawler = new Crawler();
            crawler.Crawl(topLevelUrl, reportFile);
        }
    }
}
