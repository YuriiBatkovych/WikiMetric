using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;

//Program poszukuje strone Wikipedii zawierającą słowo kluczowe, zaczynając od losowego artykułu N razy
//Wyniki poszukiwania są zapisywane w pliku AnalizingNRandomPages.txt
//Są tam też zapisywane statystyki metryki.
//W naszym przypadku słowem kluczowym jest nazwisko znanego akwarelisty.


namespace HitlerMetrics
{
    class Program
    {
        private static string WORD_TO_FIND = "Hitler";
        private static string STARTING_PAGE = "http://en.wikipedia.org/wiki/Special:Random";

        static void Main(string[] args)
        {
            Console.WriteLine("Wprowadź ile razy wykonać analize metryki znanego akwarelisty : ");
            int N = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("czekaj...");
            analizeNPagePaths(N);
            Console.WriteLine("Sprawdź plik Analizing" + N + "RandomPages.txt");
            Console.ReadKey();
        }

        private static void analizeNPagePaths(int N)
        {
            string fileName = "..\\..\\Analizing" + N + "RandomPages.txt";

            List<int> metrics = new List<int>();

            if (File.Exists(fileName))
                File.Delete(fileName);

            using (StreamWriter sw = File.AppendText(fileName)) 
            {
                for(int i=0; i<N; i++)
                {
                   // Console.WriteLine(i);
                    var Path = getPath();
                    int metric = Path.Count(); 
                    metrics.Add(metric);
                    sw.Write(metric + " : ");

                    foreach (var item in Path)
                        sw.Write(item.Page + " -> ");

                    sw.WriteLine();
                }

                sw.WriteLine();
                sw.WriteLine("===================================================");
                sw.WriteLine();

                sw.WriteLine("Statystyka : ");
                sw.WriteLine("Średnia metryka = " + metrics.Average());
                sw.WriteLine("Minimalna metryka = " + metrics.Min());
                sw.WriteLine("Maksymalna metryka = " + metrics.Max());

            }
        }

        private static LinkedList<PathType> getPath()
        {
            var tokenSource = new CancellationTokenSource();  //żeby móc anulować wszystkie wątki obliczające tę ścieżke
            var token = tokenSource.Token;                      //po jej znalezieniu

            var crawler = new Crawler(WORD_TO_FIND, STARTING_PAGE, 0, token);

            LinkedList<PathType> Path;

            try
            {
                Path = crawler.Run().Result;
                tokenSource.Cancel();
            }
            catch(Exception)
            {
                tokenSource.Cancel();  //anulowanie wątków;
                Path = getPath();
            }

            return Path;
        }

        private static void showPath(LinkedList<PathType> path)
        {
            Console.WriteLine("Path found : ");

            foreach (var item in path)
                Console.WriteLine(item.Page);
        }
    }
}
