using EnsureThat;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitStats.Console
{
    internal class AuthorAnalyser : GitAnalyser
    {
        //Number of authors in a file?
        public void Analyse(string gitPath)
        {
            var repo = OpenRepository(gitPath);
            if (repo == null) return;
            System.Console.WriteLine("OK");

            //Ignore merge commits
            var commits = repo.Commits.Where(c => !c.Message.ToLowerInvariant().StartsWith("merge"));
            var commitsProcessed = 0;
            var commitsTotal = commits.Count();

            var authorsByFile = new Dictionary<string, List<string>>();

            foreach (var commit in commits)
            {
                commitsProcessed++;
                if (commitsProcessed % 100 == 0) { System.Console.WriteLine($"Progress: {commitsProcessed}/{commitsTotal}"); }
                foreach (var parent in commit.Parents)
                {
                    foreach (var change in repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree))
                    {
                        if (authorsByFile.ContainsKey(change.Path))
                        {
                            if (!authorsByFile[change.Path].Contains(commit.Author.Email))
                                authorsByFile[change.Path].Add(commit.Author.Email);
                        }
                        else
                        {
                            authorsByFile.Add(change.Path, new List<string>() { commit.Author.Email });
                        }
                    }
                }
            }

            System.Console.WriteLine("--- Crowd Sourced Files ---");

            foreach (var file in authorsByFile.OrderByDescending(p => p.Value.Count)
                                              .Take(10))
            {
                System.Console.WriteLine($"File: {file.Key} | {file.Value.Count} authors");
                foreach (var author in file.Value)
                {
                    System.Console.WriteLine($"          | {author}");
                }
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