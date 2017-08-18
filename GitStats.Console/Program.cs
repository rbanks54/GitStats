using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitStats.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.Write("Git path: ");
            var gitPath = System.Console.ReadLine();
            System.Console.Write("Analysis ([S]entiment, [K]ey phrases, [B]ugSpots, [C]oupling, [I]mpact, [A]uthors): ");
            var analysis = System.Console.ReadLine();
            switch (analysis.ToLowerInvariant())
            {
                case "i":
                    new ImpactAnalyser().Analyse(gitPath);
                    break;
                case "s":
                    new SentimentAnalyser().Analyse(gitPath);
                    break;
                case "k":
                    new KeyPhraseAnalyser().Analyse(gitPath);
                    break;
                case "c":
                    new CouplingAnalyser().Analyse(gitPath);
                    break;
                case "a":
                    new AuthorAnalyser().Analyse(gitPath);
                    break;
                case "b":
                default:
                    new BugSpotAnalyser().Analyse(gitPath);
                    break;
            }
            System.Console.ReadLine();
        }
    }
}
