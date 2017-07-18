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
        //TODO: fix GetLegalUri function
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

        private static void Main(string[] args)
        {
            const string pageUrl = "https://stackoverflow.com/questions/8606793/how-do-i-convert-streamreader-to-a-string";
            const string downloadDir = @"D:\My\Desktop\tmp\";

            var tuple = GetHtmlDocument(pageUrl);
            var document = tuple.Item1;


            //creates folder with all images and scripts
            var fileName = GetFileName(document.DocumentNode.SelectSingleNode("//title").InnerHtml);
            Console.WriteLine(document.DocumentNode.SelectSingleNode("//title").InnerHtml);
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

        public static string GetFileName(string url)
        {
            var lastPartOfUrlRegex = new Regex("[^/]+(?=/$|$)");
            var lastPart = lastPartOfUrlRegex.Match(url).Result("$0");
            var fileName = lastPart.Split('?')[0];
            var regex = new Regex(@"[\\/:*?""<>|]");
            return regex.Replace(fileName, "");
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

                    //byte[] text = ReadFully(reader.BaseStream);

                    //text = Encoding.Convert(Encoding.GetEncoding(response.CharacterSet), Encoding.UTF8, text);

                    //File.WriteAllBytes(@"D:\My\Desktop\tmp\index.txt", text);

                    //doc.Load(@"D:\My\Desktop\tmp\index.txt");
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