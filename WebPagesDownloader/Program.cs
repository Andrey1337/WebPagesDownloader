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
using HtmlAgilityPack;

namespace WebPagesDownloader
{
    class Program
    {
        public static List<string> GetImagesLinks(HtmlDocument document, string pageUrl)
        {
            List<string> imageLinks = new List<string>();
            Uri uri = new Uri(pageUrl);

            foreach (var image in document.DocumentNode.SelectNodes("//img"))
            {
                var src = image.GetAttributeValue("src", null);
                if (!imageLinks.Contains(src))
                {
                    if (src[0] == '/' && src[1] == '/')
                        imageLinks.Add(src);
                    else
                        imageLinks.Add(uri.Authority + src);
                }
            }
            return imageLinks;
        }

        public static void ChangeAllImagesInHtml(HtmlDocument document, string filesPath)
        {            
            foreach (var item in document.DocumentNode.SelectNodes("//img"))
            {               
                var imagePath = Path.GetFileName(item.GetAttributeValue("src", null));               
                item.SetAttributeValue("src", filesPath + @"\" + imagePath);
            }
            //File.WriteAllText(path, document.DocumentNode.OuterHtml);
        }

        public static void LinkDownloader(List<string> linksUrlList, string filesPath)
        {
            foreach (var item in linksUrlList)
            {
                try
                {

                    using (WebClient client = new WebClient())
                    {
                        if (item[0] == '/')
                            client.DownloadFile("http:" + item, filesPath + @"\" + Path.GetFileName(item));
                        else
                            client.DownloadFile("http://" + item, filesPath + @"\" + Path.GetFileName(item));
                    }
                    Console.WriteLine("Succes download: " + item);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error while downloading image from: " + item + "\nexception: " + exception);
                }
            }
        }

        static void Main(string[] args)
        {
            //int threadNum = Convert.ToInt32(ConfigurationManager.AppSettings.Get("numOfThreads"));

            var pageUrl = "https://en.wikipedia.org/wiki/Dog";
            var uri = new Uri(pageUrl);

            var web = new HtmlWeb();
            var document = web.Load(pageUrl);

            var fileName = document.DocumentNode.SelectSingleNode("//title").InnerHtml;

            //TODO: check imposible charts of the directory
            //creates folder with all images and scripts
            var folderPath = fileName + "_files";
            var filesPath = @"D:\My\Desktop\tmp\" + folderPath;
            Directory.CreateDirectory(filesPath);

            var links = document.DocumentNode.SelectNodes("//link[@href]").Select(link => link.Attributes["href"].Value);
            var readyLinks = links.Where(item => item[0] == '/' && item[1] != '/').Select(item => uri.Authority + item);

            var imageLinks = GetImagesLinks(document, pageUrl);

            var path = @"D:\My\Desktop\tmp\" + fileName + ".html";

            //---Starts Download 
            LinkDownloader(imageLinks, filesPath);
            ChangeAllImagesInHtml(document, folderPath);
            
            File.WriteAllText(path, document.DocumentNode.OuterHtml);
        }

    }
}
