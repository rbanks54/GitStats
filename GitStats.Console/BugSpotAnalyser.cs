using EnsureThat;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitStats.Console
{
    internal class BugSpotAnalyser : GitAnalyser
    {
        //See Readme for https://github.com/d0vs/bugspots

        public void Analyse(string gitPath)
        {
            var repo = OpenRepository(gitPath);
            if (repo == null) return;
            System.Console.WriteLine("OK");

            //Naive - could add regex match here, catch words properly, etc.
            var commits = repo.Commits.Where(c =>
            {
                var m = c.Message.ToLowerInvariant();
                return (m.Contains("fix") || m.Contains("closed") || m.Contains("bug") || m.Contains("resolve"));
            });

            var lastCommitTime = repo.Commits.First().Committer.When;
            var firstCommitTime = repo.Commits.Last().Committer.When;
            var repoAge = lastCommitTime - firstCommitTime;

            //Get a list of all files in bug fix commits and when the file was changed
            var fileFixOccurrences = new Dictionary<string, List<DateTimeOffset>>();

            foreach (var commit in commits)
            {
                foreach (var parent in commit.Parents)
                {
                    foreach (var change in repo.Diff.Compare<TreeChanges>(parent.Tree,
                    commit.Tree))
                    {
                        if (fileFixOccurrences.ContainsKey(change.Path))
                        {
                            fileFixOccurrences[change.Path].Add(commit.Committer.When);
                        }
                        else
                        {
                            fileFixOccurrences.Add(change.Path, new List<DateTimeOffset>() { commit.Committer.When });
                        }
                    }
                }
            }

            //Now score up the files, weighted by age of when change occurred
            var fileFixScores = new Dictionary<string, Tuple<double,int>>();

            foreach (var fileFixSet in fileFixOccurrences)
            {
                var score = fileFixSet.Value.Sum(changedDate => {
                    var ageDistance = (changedDate - firstCommitTime).TotalMinutes / repoAge.TotalMinutes;
                    //weight the age - the futher back in time, the lower the weighting
                    return 1 / (1 + Math.Exp(-12 * ageDistance + 12));
                });
                fileFixScores.Add(fileFixSet.Key, new Tuple<double,int>(score,fileFixSet.Value.Count()));
            }

            foreach (var file in fileFixScores.OrderByDescending(pair => pair.Value).Take(10))   
            {
                System.Console.WriteLine($"File: {file.Key} | {file.Value.Item1} ({file.Value.Item2})");
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