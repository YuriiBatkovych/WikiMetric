using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace HitlerMetrics
{
    public class PathType
    {
        public string Page;

        public PathType(string page)
        {
            Page = page;
        }
    }

    public class Crawler
    { 
        private string _startLink;
        private string _toFind;
        private CancellationToken _token;  //token cancelacji jedyny dla wszystkich wątków które szukają tę samą ścieżkę
        private int _depth;

        public Crawler() { }

        public Crawler(string ToFind, string StartLink, int depth, CancellationToken token)
        {
            _toFind = ToFind;
            _startLink = StartLink;
            _token = token;
            _depth = depth;
        }

        private bool checkPage(string text) //sprawdzanie czy text strony zawiera poszukiwane hasło
        {
            return text.Contains(_toFind);
        }
        
        private LinkedList<PathType> searchEachLinkAsync(List<string> links) //szukamy słowa kluczowego w każdym linku 
        { 
            var distances = new LinkedList<Task<LinkedList<PathType>>>();  // lista tasków zwracających ścieżkę

            foreach (var page in links)
            {
                var crawler = new Crawler(_toFind, page, _depth+1, _token);
                distances.AddLast(crawler.Run());
            }

            Task.WaitAny (distances.ToArray());  //czekamy póki ktoś znajdzie ścieżkę 

            while (true)
            { 
                if (distances.Where(x => x!=null && x.IsCompleted).Any())
                {
                    var completed = distances.Where(x => x != null && x.IsCompleted)
                            .Select(x => x.Result);
                    int minDist = completed.Select(x => x.Count()).Min();

                    return completed.Where(x => x.Count() == minDist).First();   //wybieramy jakąkolwiek najmniejszą ścieżkę
                }
            }
        }

        public Task<LinkedList<PathType>> Run()
        {
            if (_depth > 15) return null;

            return Task.Run(() =>
            {
                var webGet = new HtmlWeb();
                var document = webGet.Load(_startLink);   //dokument html opisujący aktykuł wikipedii

                if (checkPage(document.DocumentNode.InnerText)) //najpierw szukamy słowa kluczowego w swoim artykule
                {
                    var Path = new LinkedList<PathType>();
                    Path.AddLast(new PathType(_startLink));
                    return Path;
                }
                else  //potem w "artykułach podrzędnych"
                {
                    var links = findLinks(document);
                    var Path = searchEachLinkAsync(links);
                    Path.AddFirst(new PathType(_startLink));
                    return Path;
                }
            }, _token);
        }

        private bool checkLink(string link) // sprawdzamy czy link jest na wikipediową strone i czy nie jest linkiem specjalnym
        {                                                                                       //(File: , Catagory: i td)
            if (link.Length > 6)
            {
                string linkStart = link.Substring(0, 6);
                return linkStart.Equals("/wiki/") && !link.Contains(':');
            }
            else return false;
        }

        private List<string> ExtractAllAHrefTags(HtmlDocument htmlSnippet) //wybieramy wszystkie <a href = " ...> tagi z documentu
        {
            List<string> hrefTags = new List<string>();

            foreach (HtmlNode link in htmlSnippet.DocumentNode.SelectNodes("//a[@href]"))
            {
                HtmlAttribute att = link.Attributes["href"];
                hrefTags.Add(att.Value);
            }

            return hrefTags;
        }

        private List<string> findLinks(HtmlDocument doc)
        {
            var tags = ExtractAllAHrefTags(doc);

            var links = tags.Where(x => checkLink(x))
                .Distinct()
                .Select(x => "https://en.wikipedia.org" + x)
                .ToList();

            return links;
        }
    }
}
