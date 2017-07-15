using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebPagesDownloader
{
    class Program
    {
        //TODO: fix GetLegalUri function
        public static string GetLegalUri(string uri, string pageUrl)
        {
            if (uri.Contains("http"))
            {
                return uri;
            }
            if (uri[0] == '/' && uri[1] == '/')
            {
                return "https:" + uri;
            }

            return "https://" + new Uri(pageUrl).Authority + uri;
        }

        public static List<string> GetLinksWithArgument(HtmlDocument document, string pageUrl, string nodeName, string subNodeName)
        {
            return document.DocumentNode.SelectNodes(nodeName).Select(link => link.Attributes[subNodeName]?.Value).Select(link => GetLegalUri(link, pageUrl)).ToList();
        }

        public static void ChangeSubNodesInHtml(HtmlDocument document, string filesPath, string nodeName, string subNodeName, string arg = "default")
        {
            var linkCounter = new Dictionary<string, int>();

            foreach (var node in document.DocumentNode.SelectNodes(nodeName))
            {
                var link = node.GetAttributeValue(subNodeName, null);
                if (link == null)
                    continue;
                if (arg != "default")
                    link = Path.GetFileNameWithoutExtension(GetFileName(link)) + arg;

                if (linkCounter.ContainsKey(GetFileName(link)))
                {
                    linkCounter[GetFileName(link)]++;
                    node.SetAttributeValue(subNodeName, filesPath + @"\" + Path.GetFileNameWithoutExtension(GetFileName(link)) + "_" + linkCounter[GetFileName(link)] + Path.GetExtension(GetFileName(link)));
                }
                else
                {
                    linkCounter.Add(GetFileName(link), 1);
                    node.SetAttributeValue(subNodeName, filesPath + @"\" + Path.GetFileName(GetFileName(link)));
                }
            }
        }

        public static void GetJavascript(HtmlDocument document)
        {
            foreach (var link in document.DocumentNode.SelectNodes("//script").Select(link => link.Attributes["src"]?.Value))
            {
                Console.WriteLine(link);
            }
        }

        public static void LinkDownloader(List<string> linksUrlList, string filesPath, string extension = "default")
        {
            var sameLinksCounter = new Dictionary<string, int>();
            foreach (var item in linksUrlList)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string path = GetFileName(item);

                        if (extension != "default")
                        {
                            path = Path.GetFileNameWithoutExtension(path) + extension;
                        }

                        if (!sameLinksCounter.ContainsKey(path))
                        {
                            sameLinksCounter.Add(path, 1);
                            client.DownloadFile(item, filesPath + @"\" + path);
                        }
                        else
                        {
                            sameLinksCounter[path]++;
                            client.DownloadFile(item, filesPath + @"\" + Path.GetFileNameWithoutExtension(path) + "_" + sameLinksCounter[path] + Path.GetExtension(path));
                        }

                    }
                    Console.WriteLine("Succes to download: " + item);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error while downloading file from: " + item + "\n exception: " + exception);
                }
            }
        }

        static void Main(string[] args)
        {
            const string pageUrl = "https://en.wikipedia.org/wiki/Cat";
            const string downloadDir = @"D:\My\Desktop\tmp\";

            var web = new HtmlWeb();
            var document = web.Load(pageUrl);

            //creates folder with all images and scripts
            var fileName = GetFileName(document.DocumentNode.SelectSingleNode("//title").InnerHtml);
            var path = downloadDir + fileName + ".html";
            var folderPath = fileName + "_files";
            var filesPath = downloadDir + folderPath;
            Directory.CreateDirectory(filesPath);


            //<---Search links of images and css--->
            var imageLinks = GetLinksWithArgument(document, pageUrl, "//img", "src");
            var scriptLinks = GetLinksWithArgument(document, pageUrl, "//script[@src]", "src");
            var cssLinks = GetLinksWithArgument(document, pageUrl, "//link[@rel='stylesheet']", "href");

            //<---Starts Download--->
            Console.WriteLine("Images:");
            LinkDownloader(imageLinks, filesPath);
            Console.WriteLine("Css:");
            LinkDownloader(cssLinks, filesPath, ".css");
            Console.WriteLine("Scripts:");
            LinkDownloader(scriptLinks, filesPath);

            ChangeSubNodesInHtml(document, folderPath, "//img", "src");
            ChangeSubNodesInHtml(document, folderPath, "//link[@rel='stylesheet']", "href", ".css");
            ChangeSubNodesInHtml(document, folderPath, "//script[@src]", "src");
            File.WriteAllText(path, document.DocumentNode.OuterHtml);
        }

        public static string GetFileName(string url)
        {
            var lastPartOfUrlRegex = new Regex("[^/]+(?=/$|$)");
            var lastPart = lastPartOfUrlRegex.Match(url).Result("$0");
            var fileName = lastPart.Split('?')[0];
            var regex = new Regex(@"[\\/:*?""<>|]");
            return regex.Replace(fileName, "");
        }
    }
}