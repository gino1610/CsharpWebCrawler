using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WebCrawler
{
    public class Crawler
    {
        private List<string> CrawledPages = new List<string>();
        private List<string> linkDB = new List<string>();
        private Node nodeDB = new Node();
        private Node extNodeDB = new Node();
        private string TopLevelUrl;


        public Crawler()
        {

        }

        public void Crawl(string topLevelUrl, string reportFile)
        {
            var robotsFile = topLevelUrl + "/robots.txt";
            TopLevelUrl = topLevelUrl;
            nodeDB.Link = topLevelUrl;

            var sitemaps = ProcessRobotsFile(robotsFile);

            foreach (var sitemap in sitemaps)
            {
                ProcessSiteMapFile(sitemap);
            }

            BuildHierachyTree();
            // CrawlPage(topLevelUrl);
            CreateReport(reportFile);
            var reportFile2 = reportFile.Replace(".txt", "2.txt");
            CreateTreeReport(reportFile2);
        }

        private void CreateTreeReport(string reportFile2)
        {
            var activeNode = nodeDB;

            List<string> displayStr = new List<string>();
            int level = 1;
            displayStr.Add(nodeDB.Link);
            printNode(nodeDB, ref displayStr, level++);


            Console.WriteLine("CreateReport(" + reportFile2 + ")");

            using (StreamWriter writer = new StreamWriter(reportFile2))
            {
                foreach (var str in displayStr)
                {
                    writer.WriteLine(str);
                }
            }
        }

        private void printNode(Node activeNode, ref List<string> displayStr, int level)
        {
            var tabstr = "";
            for (int ii = 0; ii < level; ii++)
                tabstr += "    ";

            level++;

            foreach (var childNode in activeNode.ChildNodes)
            {
                
                displayStr.Add(tabstr + childNode.Link);
                printNode(childNode, ref displayStr, level);
                
            }
        }


        private void BuildHierachyTree()
        {
            linkDB.Distinct().OrderBy(x => x).ToList();

            foreach (var link in linkDB)
            {
                if (link.Contains(TopLevelUrl))
                {

                    var linkTokens = link.Replace(TopLevelUrl, "").Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                    var activeNode = nodeDB;
                    foreach (var token in linkTokens)
                    {
                        var childNode = activeNode.ChildNodes.Where(x => x.Link == token).FirstOrDefault();

                        if (childNode == null)
                        {
                            childNode = new Node(token);
                            activeNode.ChildNodes.Add(childNode);
                        }
                        activeNode = childNode;
                }
                }
            }

        }

        private List<string> ProcessRobotsFile(string robotsFile)
        {
            var sitemaps = new List<string>();
            if (!string.IsNullOrWhiteSpace(robotsFile))
            {
                var robotsPage = GetWebText(robotsFile);
                sitemaps = ParseRobotsFile(robotsPage);
            }
            return sitemaps;
        }

        private List<string> ParseRobotsFile(string robotsPage)
        {
            var sitemaps = new List<string>();
            var sitemapLinePattern = @"Sitemap: * http.*\.xml";
            var sitemapPattern = @"http.*\.xml";

            var matches = Regex.Matches(robotsPage, sitemapLinePattern);

            foreach (var match in matches)
            {
                var sm = Regex.Match(match.ToString(), sitemapPattern);
                if(!string.IsNullOrWhiteSpace(sm.Value))
                {
                    sitemaps.Add(sm.Value.Trim());
                }
            }
            return sitemaps;
        }

        private void ProcessSiteMapFile(string siteMapFile)
        {
            if (!string.IsNullOrWhiteSpace(siteMapFile))
            {
                var siteMapXmlPage = GetWebText(siteMapFile);
                var siteMapXml = ParseSiteMapFile(siteMapXmlPage);

                if (siteMapXml.ContainsKey("link") && siteMapXml["link"].Count > 0)
                {
                    var links = siteMapXml["link"];
                    linkDB.AddRange(links);
                }

                if (siteMapXml.ContainsKey("sitemap") && siteMapXml["sitemap"].Count > 0)
                {
                    var sitemapXmls = siteMapXml["sitemap"];
                    foreach (var sm in sitemapXmls)
                    {
                        ProcessSiteMapFile(sm);
                    }
                }
            }
        }

        private Dictionary<string, List<string>> ParseSiteMapFile(string siteMapPage)
        {
            Dictionary<string, List<string>> links = new Dictionary<string, List<string>>();
            links.Add("sitemap", new List<string>());
            links.Add("link", new List<string>());


            XDocument xDoc = new XDocument();
            string xmlNamespacePattern = @"xmlns(:\w+)?=""([^""]+)""|xsi(:\w+)?=""([^""]+)""";
            var siteMapXmlPage = Regex.Replace(siteMapPage, xmlNamespacePattern, "");
            xDoc =  XDocument.Parse(siteMapXmlPage);
            var junkie = xDoc.Descendants("loc").Select(x => x.Value).ToList();
            foreach (XElement element in xDoc.Descendants("loc"))
            {
                var link = element.Value.Trim();
                // Console.WriteLine(element.Value);
                if (link.EndsWith(".xml"))
                {
                    var sitemaps = links["sitemap"];
                    if (!sitemaps.Any(x => x == link))
                        sitemaps.Add(link);
                }
                else
                {
                    var weblinks = links["link"];
                    if (!weblinks.Any(x => x == link))
                        weblinks.Add(link);
                }

            }

            return links;
        }

        public void CrawlPage(string url)
        {
            if (!IsPageAlreadyCrawled(url))
            {
                CrawledPages.Add(url);

                Console.WriteLine("CrawlPage(" + url + ") Start");

                var htmlText = GetWebText(url);
                Console.WriteLine(htmlText);

                Console.WriteLine("CrawlPage(" + url + ") End");
            }
        }

        private bool IsPageAlreadyCrawled(string url)
        {
            return CrawledPages.Any(x => x == url);
        }

        public void CreateReport(string reportFile)
        {
            Console.WriteLine("CreateReport(" + reportFile + ")");
            using (StreamWriter writer = new StreamWriter(reportFile))
            {
                foreach(var link in linkDB)
                {
                    writer.WriteLine(link);
                }
            }
        }

        public static string GetWebText(string url)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.UserAgent = "A Web Crawler";

            WebResponse response = request.GetResponse();

            Stream stream = response.GetResponseStream();

            StreamReader reader = new StreamReader(stream);
            string htmlText = reader.ReadToEnd();
            return htmlText;
        }
    }


    public class Node
    {
        public string Link { get; set; }
        public List<Node> ChildNodes { get; set; } = new List<Node>();

        public Node() { }

        public Node(string link)
        {
            Link = link;
        }
    }
}
