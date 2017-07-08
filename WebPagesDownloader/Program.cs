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
        public static List<string> GetLinksWithArgument(HtmlDocument document, string pageUrl, string argument)
        {
            List<string> imageLinks = new List<string>();

            foreach (var image in document.DocumentNode.SelectNodes(argument).Select(link => link.Attributes["src"]?.Value))
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

        public static List<string> GetCssLinks(HtmlDocument document, string pageUrl)
        {
            List<string> cssLinks = new List<string>();

            foreach (var cssLink in document.DocumentNode.SelectNodes("//link[@rel]").Where(link => link.Attributes["rel"].Value == "stylesheet").Select(link => link.Attributes["href"].Value))
            {
                //TODO: normal uri creater 

                if (cssLink.Contains("http"))
                {
                    cssLinks.Add(cssLink);
                }
                else if (cssLink[0] == '/' && cssLink[1] == '/')
                {
                    cssLinks.Add("https:" + cssLink);
                }
                else
                {
                    cssLinks.Add("https://" + new Uri(pageUrl).Host + cssLink);
                }

            }
            return cssLinks;
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
                        Console.WriteLine(item);
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
                    //Console.WriteLine("Succes download: " + item);
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

            var pageUrl = "https://stackoverflow.com/questions/13565069/parse-relative-uri";

            var web = new HtmlWeb();
            var document = web.Load(pageUrl);

            //TODO: check imposible charts of the directory
            //creates folder with all images and scripts
            var fileName = document.DocumentNode.SelectSingleNode("//title").InnerHtml;
            var path = @"D:\My\Desktop\tmp\" + fileName + ".html";
            var folderPath = fileName + "_files";
            var filesPath = @"D:\My\Desktop\tmp\" + folderPath;
            Directory.CreateDirectory(filesPath);

            //<---Search links of images and css--->
            var imageLinks = GetLinksWithArgument(document, pageUrl, "//img");
            var scriptLinks = GetLinksWithArgument(document, pageUrl, "//script[@src]");
            var cssLinks = GetCssLinks(document, pageUrl);

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