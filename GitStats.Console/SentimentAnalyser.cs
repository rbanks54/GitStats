using EnsureThat;
using LibGit2Sharp;
using System;
using System.Linq;
using Microsoft.ProjectOxford.Text.Sentiment;
using System.Collections.Generic;

namespace GitStats.Console
{
    internal class SentimentAnalyser : GitAnalyser
    {
        public SentimentAnalyser()
        {
        }

        public void Analyse(string gitPath)
        {
            var repo = OpenRepository(gitPath);
            if (repo == null) return;
            System.Console.WriteLine("OK");

            var commits = repo.Commits.Where(c => !c.Message.StartsWith("Merge")).Take(20);
            if (commits.Count() == 0)
            {
                System.Console.WriteLine("-- No data --");
            }

            var request = new SentimentRequest();

            foreach (var commit in commits)
            {
                var document = new SentimentDocument()
                {
                    Id = commit.Id.Sha,
                    Text = commit.Message,
                    Language = "en"
                };
                request.Documents.Add(document);
            }

            var apiKey = System.Configuration.ConfigurationManager.AppSettings["ApiKey"];
            var client = new SentimentClient(apiKey);

            var response = client.GetSentiment(request);
            var scores = new List<float>();

            foreach (var doc in response.Documents)
            {
                scores.Add(doc.Score);
                System.Console.WriteLine($"Id: {doc.Id} | Score: {(doc.Score * 100)}%");
                if (doc.Score < 0.3 || 0.8 < doc.Score)
                {
                    System.Console.WriteLine($"\t --> {request.Documents.FirstOrDefault(d => d.Id == doc.Id).Text}");
                }
            }
            System.Console.WriteLine($"Average score: {(scores.Average() * 100)}");
        }

        private Repository OpenRepository(string gitPath)
        {
            EnsureArg.IsNotNullOrWhiteSpace(gitPath);
            if (Repository.IsValid(gitPath))
            {
                return new Repository(gitPath);
            };
            return null;
        }
    }
}