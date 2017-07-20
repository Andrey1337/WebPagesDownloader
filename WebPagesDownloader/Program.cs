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
using System.Web;
using System.Web.UI;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace WebPagesDownloader
{
    class Program
    {
        public static string GetAbsoluteUrl(string baseLink, string myLink)
        {
            Uri baseUri = new Uri(baseLink);
            Uri myUri = new Uri(baseUri, myLink);
            return myUri.ToString();
        }

        public static List<string> GetLinksWithArgument(HtmlDocument document, string pageUrl, string nodeName, string subNodeName)
        {
            return document.DocumentNode.SelectNodes(nodeName).Select(link => link.Attributes[subNodeName]?.Value)
                .Select(link => GetAbsoluteUrl(pageUrl, link)).ToList();
        }

        public static void ChangeSubNodesInHtml(HtmlDocument document, string filesPath, string nodeName, string subNodeName, string arg = "default")
        {
            var linkCounter = new Dictionary<string, int>();

            foreach (var node in document.DocumentNode.SelectNodes(nodeName))
            {
                var link = node.GetAttributeValue(subNodeName, null);

                if (link == null)
                    continue;

                string fileName = GetFileNameFromLink(link);
                if (arg != "default")
                    fileName = Path.GetFileNameWithoutExtension(fileName) + arg;

                if (linkCounter.ContainsKey(fileName))
                {
                    linkCounter[fileName]++;
                    string pathName = Path.Combine(filesPath,
                        Path.GetFileNameWithoutExtension(fileName) + "_" + linkCounter[fileName] +
                        Path.GetExtension(fileName));
                    node.SetAttributeValue(subNodeName, pathName);
                }
                else
                {
                    linkCounter.Add(fileName, 1);
                    string pathName = Path.Combine(filesPath, Path.GetFileName(fileName));
                    node.SetAttributeValue(subNodeName, pathName);
                }
            }
        }


        public static void LinkDownloader(List<string> linksUrlList, string filesPath, string extension = "default")
        {
            var sameLinksCounter = new Dictionary<string, int>();
            foreach (var link in linksUrlList)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string fileName = GetFileNameFromLink(link);

                        if (extension != "default")
                        {
                            fileName = Path.GetFileNameWithoutExtension(fileName) + extension;
                        }

                        if (!sameLinksCounter.ContainsKey(fileName))
                        {
                            sameLinksCounter.Add(fileName, 1);
                            client.DownloadFile(link, filesPath + @"\" + fileName);
                        }
                        else
                        {
                            sameLinksCounter[fileName]++;
                            client.DownloadFile(link, filesPath + @"\" + Path.GetFileNameWithoutExtension(fileName) + "_" + sameLinksCounter[fileName] + Path.GetExtension(fileName));
                        }
                    }
                    Console.WriteLine("Succes to download:" + link);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(GetFileNameFromLink(link));
                    Console.WriteLine("Error while downloading file from: " + link + "\n exception: " + exception);
                }
            }
        }

        private static void Main(string[] args)
        {
            string pageUrl = ConfigurationManager.AppSettings.Get("pageUrl");
            const string downloadDir = @"D:\My\Desktop\tmp\";

            //var tuple = GetHtmlDocument(pageUrl);
            //var document = tuple.Item1;
            var web = new HtmlWeb();
            var document = web.Load(pageUrl);

            //<---Creates folder with all images and scripts--->
            var fileName = GetFileNameFromTitle(document.DocumentNode.SelectSingleNode("//title").InnerHtml);

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

            //<---Change SubNodes--->
            ChangeSubNodesInHtml(document, folderPath, "//img", "src");
            ChangeSubNodesInHtml(document, folderPath, "//link[@rel='stylesheet']", "href", ".css");
            ChangeSubNodesInHtml(document, folderPath, "//script[@src]", "src");
            File.WriteAllText(path, document.DocumentNode.OuterHtml);
        }

        public static string GetFileNameFromLink(string url)
        {
            var lastPartOfUrlRegex = new Regex("[^/]+(?=/$|$)");
            var lastPart = lastPartOfUrlRegex.Match(url).Result("$0");
            var fileName = lastPart.Split('?')[0];
            return new Regex(@"[\\/:""*?<>|]").Replace(fileName, "");
        }

        public static string GetFileNameFromTitle(string url)
        {
            return new Regex(@"[\\/:*?<>|]").Replace(url, "");
        }

        public static Tuple<HtmlDocument, string> GetHtmlDocument(string url)
        {
            string charset;
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            HtmlDocument doc = new HtmlDocument();
            using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    //var text = reader.ReadToEnd();
                    //File.WriteAllText(@"D:\My\Desktop\tmp\index.txt", text, Encoding.GetEncoding(response.CharacterSet));
                    doc.Load(reader);
                    charset = response.CharacterSet;
                }
            }
            return new Tuple<HtmlDocument, string>(doc, charset);
        }
    }
}