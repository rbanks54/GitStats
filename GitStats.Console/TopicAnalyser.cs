using EnsureThat;
using LibGit2Sharp;
using System;
using System.Linq;
using Microsoft.ProjectOxford.Text.Topic;
using System.Collections.Generic;
using System.Threading;

namespace GitStats.Console
{
    internal class TopicAnalyser : GitAnalyser
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

            var request = new TopicRequest();

            foreach (var commit in commits)
            {
                var document = new TopicDocument()
                {
                    Id = commit.Id.Sha,
                    Text = commit.Message,
                    Language = "en"
                };
                request.Documents.Add(document);
            }

            var apiKey = System.Configuration.ConfigurationManager.AppSettings["ApiKey"];
            var client = new TopicClient(apiKey);

            var operationUrl = client.StartTopicProcessing(request);

            TopicResponse response = null;
            var doneProcessing = false;

            while (!doneProcessing)
            {
                response = client.GetTopicResponse(operationUrl);

                switch (response.Status)
                {
                    case TopicOperationStatus.Cancelled:
                        System.Console.WriteLine("Status: Operation Cancelled");
                        doneProcessing = true;
                        break;
                    case TopicOperationStatus.Failed:
                        System.Console.WriteLine("Status: Operation Failed");
                        doneProcessing = true;
                        break;
                    case TopicOperationStatus.NotStarted:
                        System.Console.WriteLine("Status: Operation Not Started");
                        Thread.Sleep(5000);
                        break;
                    case TopicOperationStatus.Running:
                        System.Console.WriteLine("Status: Operation Running");
                        Thread.Sleep(2000);
                        break;
                    case TopicOperationStatus.Succeeded:
                        System.Console.WriteLine("Status: Operation Succeeded");
                        doneProcessing = true;
                        break;
                }
            }

            foreach (var topic in response.OperationProcessingResult.Topics)
            {
                System.Console.WriteLine($"Topic: {topic.KeyPhrase} | {(topic.Score * 100)}%");
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