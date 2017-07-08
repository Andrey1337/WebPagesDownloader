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
        public static List<string> GetLinksWithArgument(HtmlDocument document, string pageUrl, string nodeName, string subNodeName)
        {
            List<string> imageLinks = new List<string>();

            foreach (var image in document.DocumentNode.SelectNodes(nodeName).Select(link => link.Attributes[subNodeName]?.Value))
            {
                //TODO: Create normal uri creater
                if (image.Contains("http"))
                {
                    imageLinks.Add(image);
                }
                else if (image[0] == '/' && image[1] == '/')
                {
                    imageLinks.Add("https:" + image);
                }
                else
                {
                    imageLinks.Add("https://" + new Uri(pageUrl).Authority + image);
                }
            }
            return imageLinks;
        }

        public static void ChangeAllImagesInHtml(HtmlDocument document, string filesPath)
        {
            foreach (var item in document.DocumentNode.SelectNodes("//img"))
            {
                var imagePath = GetFileName(item.GetAttributeValue("src", null));
                item.SetAttributeValue("src", filesPath + @"\" + imagePath);
            }
        }

        public static void ChangeAllCssInHtml(HtmlDocument document, string filesPath)
        {
            var cssLinkCounter = new Dictionary<string, int>();

            foreach (var cssLink in document.DocumentNode.SelectNodes("//link[@rel]").Where(link => link.Attributes["rel"].Value == "stylesheet"))
            {
                var link = cssLink.GetAttributeValue("href", null);

                if (cssLinkCounter.ContainsKey(GetFileName(link)))
                {
                    cssLinkCounter[GetFileName(link)]++;
                    cssLink.SetAttributeValue("href", filesPath + @"\" + Path.GetFileNameWithoutExtension(GetFileName(link)) + "_" + cssLinkCounter[GetFileName(link)] + Path.GetExtension(GetFileName(link)));

                }
                else
                {
                    cssLinkCounter.Add(GetFileName(link), 1);
                    cssLink.SetAttributeValue("href", filesPath + @"\" + GetFileName(link));
                }
            }
        }

        public static void LinkDownloader(List<string> linksUrlList, string filesPath)
        {
            Dictionary<string, int> sameLinksCounter = new Dictionary<string, int>();
            foreach (var item in linksUrlList)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {                        
                        var path = GetFileName(item);
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
            //int threadNum = Convert.ToInt32(ConfigurationManager.AppSettings.Get("numOfThreads"));

            var pageUrl = "https://en.wikipedia.org/wiki/Dog";

            var web = new HtmlWeb();
            var document = web.Load(pageUrl);

            //TODO: check imposible charts of the directory
            //creates folder with all images and scripts
            var fileName = document.DocumentNode.SelectSingleNode("//title").InnerHtml;
            var path = @"D:\My\Desktop\tmp\" + fileName + ".html";
            var folderPath = fileName + "_files";
            var filesPath = @"D:\My\Desktop\tmp\" + folderPath;
            Directory.CreateDirectory(filesPath);

            Test();

            //<---Search links of images and css--->
            var imageLinks = GetLinksWithArgument(document, pageUrl, "//img", "src");
            var scriptLinks = GetLinksWithArgument(document, pageUrl, "//script[@src]", "src");
            var cssLinks = GetLinksWithArgument(document, pageUrl,"//link[@rel='stylesheet']", "href");

            //<---Starts Download--->
            LinkDownloader(imageLinks, filesPath);
            LinkDownloader(cssLinks, filesPath);
            LinkDownloader(scriptLinks, folderPath);
            ChangeAllImagesInHtml(document, folderPath);
            ChangeAllCssInHtml(document, folderPath);
            File.WriteAllText(path, document.DocumentNode.OuterHtml);
        }

        public static void Test()
        {
            using (WebClient client = new WebClient())
            {
                var path = GetFileName(
                    "https://en.wikipedia.org/w/load.php?debug=false&lang=en&modules=startup&only=scripts&skin=vector");
                client.DownloadFile("https://en.wikipedia.org/w/load.php?debug=false&lang=en&modules=startup&only=scripts&skin=vector", @"D:\My\Desktop\tmp" + @"\" + path);

            }
        }

        public static string GetFileName(string url)
        {

            var lastPartOfUrlRegex = new Regex("[^/]+(?=/$|$)");
            string lastPart = lastPartOfUrlRegex.Match(url).Result("$0");
            //var untilSymbol = new Regex(".+?(?="+Regex.Escape("?")+")");
            //var fileName = untilSymbol.Match(lastPart).Result("$0");
            var removeIllegalChars = new Regex("[^a-zA-Z0-9.-]");
            var fileName = lastPart.Split('?')[0];
            return removeIllegalChars.Replace(fileName, "");
        }
    }
}