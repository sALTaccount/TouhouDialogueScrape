using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace TouhouScrape
{
    class Program
    {
        static void Main(string[] args)
        {
            //pages 1-3
            var urls1 = GetPageUrls("https://www.thpatch.net/w/index.php?title=Category:Pages_in_English");
            var urls2 = GetPageUrls("https://www.thpatch.net/w/index.php?title=Category:Pages_in_English&pagefrom=Th123%2FYuyuko%27s+Win+Quotes%2Fen#mw-pages");
            var urls3 = GetPageUrls("https://www.thpatch.net/w/index.php?title=Category:Pages_in_English&pagefrom=Th155%2FYukari%27s+Win+Quotes%2Fen#mw-pages");
            //create a list of all of the page URLs
            var urls = urls1.Concat(urls2).Concat(urls3);

            List<Dialogue> allPagesDialogue = new List<Dialogue>();

            foreach (string url in urls)
            {
                //attempt to read the page HTML, skips if it errors
                try
                {
                    List<Dialogue> pageDialogue = ReadPageDialogue("https://www.thpatch.net" + url);
                    allPagesDialogue.AddRange(pageDialogue);
                }
                catch
                {
                    Console.WriteLine("Couldn't read " + url);
                }
            }

            string jsonString = JsonConvert.SerializeObject(allPagesDialogue);
            using (StreamWriter writer = new StreamWriter("dialogue.json"))
            {
                writer.Write(jsonString);
            }
            Console.WriteLine("Read complete!");
            Console.ReadLine();
        }


        public static List<string> GetPageUrls(string url)
        {
            var pageUrls = new List<string>();
            Regex urlRx = new Regex("<a href=\"(.*?en)\" title=");
            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load(url);
            var urlMatches = urlRx.Matches(document.DocumentNode.InnerHtml);
            foreach (Match m in urlMatches)
            {
                //using index 1 since we are using capturing groups in the regex
                pageUrls.Add(m.Groups[1].Value);
            }
            return pageUrls;
        }

        public static List<Dialogue> ReadPageDialogue(string pageUrl)
        {
            var dialogueList = new List<Dialogue>();
            Console.WriteLine("Reading " + pageUrl);
            //match all dialogue
            Regex dialogueRx = new Regex("</code>([\\S\\s]*?)</p>");
            //match all names
            Regex namesRx = new Regex("<p><span lang=\"en\">([\\S\\s]*?)</span>");

            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load(pageUrl);

            //finds all <tr> nodes
            HtmlNode[] nodes = document.DocumentNode.SelectNodes("//tr").ToArray();

            int usedNodes = 0;
            foreach (HtmlNode item in nodes)
            {
                //extract name and dialogue from each <tr> node, and discard if a name and dialogue is not found
                string curHtml = item.InnerHtml;
                Match dialogueMatches = dialogueRx.Match(curHtml);
                Match namesMatches = namesRx.Match(curHtml);
                if (dialogueMatches.Groups.Count != 2 || namesMatches.Groups.Count != 2)
                {
                    //skip if there isn't text captured for either the dialogue, name, or both
                    //we are using 2 as the length since we are using capturing groups in the regex
                    continue;
                }
                usedNodes++;
                //add each dialogue segment to the dialogue list, the regex removes any extra HTML tags that got left in
                //for example, sometimes the text will be: This is a <i>test<i> message
                //and we regex it out to become: this is a test message
                dialogueList.Add(new Dialogue(Regex.Replace(namesMatches.Groups[1].Value, "<.*?>", ""), Regex.Replace(dialogueMatches.Groups[1].Value, "<.*?>", "")));
            }
            Console.WriteLine(nodes.Length + " nodes, found dialogue in " + usedNodes + "; " + (nodes.Length - usedNodes) + " discarded\n");
            return dialogueList;
        }

    }
}
