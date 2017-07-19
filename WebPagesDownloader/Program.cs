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

                if (arg != "default")
                    link = Path.GetFileNameWithoutExtension(GetFileNameFromLink(link)) + arg;

                if (linkCounter.ContainsKey(GetFileNameFromLink(link)))
                {
                    linkCounter[GetFileNameFromLink(link)]++;
                    string pathName = Path.Combine(filesPath,
                        Path.GetFileNameWithoutExtension(GetFileNameFromLink(link)) + "_" + linkCounter[GetFileNameFromLink(link)] +
                        Path.GetExtension(GetFileNameFromLink(link)));
                    node.SetAttributeValue(subNodeName, pathName);
                }
                else
                {
                    linkCounter.Add(GetFileNameFromLink(link), 1);
                    string pathName = Path.Combine(filesPath, Path.GetFileName(GetFileNameFromLink(link)));
                    node.SetAttributeValue(subNodeName, pathName);
                }
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
                        string path = GetFileNameFromLink(item);

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

        private static void Main(string[] args)
        {
            const string pageUrl = "https://stackoverflow.com/questions/60174/how-can-i-prevent-sql-injection-in-php?noredirect=1&lq=1";
            const string downloadDir = @"D:\My\Desktop\tmp\";

            //var tuple = GetHtmlDocument(pageUrl);
            //var document = tuple.Item1;
            var web = new HtmlWeb();
            var document = web.Load(pageUrl);


            //creates folder with all images and scripts
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
            var fileName = Path.GetFileName(lastPart);                   
            return new Regex(@"[\\/:*?<>|]").Replace(fileName, "");
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

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

    }
}
