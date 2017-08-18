using EnsureThat;
using LibGit2Sharp;
using System;
using System.Linq;
using Microsoft.ProjectOxford.Text.KeyPhrase;
using System.Collections.Generic;
using System.Threading;

namespace GitStats.Console
{
    internal class KeyPhraseAnalyser : GitAnalyser
    {
        public void Analyse(string gitPath)
        {
            var repo = OpenRepository(gitPath);
            if (repo == null) return;
            System.Console.WriteLine("OK");

            //Must have min of 100 docs for Topic API
            var commits = repo.Commits.Where(c => !c.Message.StartsWith("Merge")).Take(100);
            if (commits.Count() == 0)
            {
                System.Console.WriteLine("-- No data --");
            }

            var request = new KeyPhraseRequest();

            foreach (var commit in commits)
            {
                var document = new KeyPhraseDocument()
                {
                    Id = commit.Id.Sha,
                    Text = commit.Message,
                    Language = "en"
                };
                request.Documents.Add(document);
            }

            var apiKey = System.Configuration.ConfigurationManager.AppSettings["ApiKey"];
            var client = new KeyPhraseClient(apiKey);

            var response = client.GetKeyPhrases(request);

            var phrases = response.Documents.SelectMany(d => d.KeyPhrases).ToList();

            var phraseStats = phrases.Distinct().Select(p => new { phrase = p, count = phrases.Count(x => x.Equals(p))});

            foreach (var result in phraseStats.OrderBy(ps => ps.count).Take(10))
            {
                System.Console.WriteLine($"Key Phrase: {result.phrase} | Occurrences: {result.count}");
            }
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