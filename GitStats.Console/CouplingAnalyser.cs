using EnsureThat;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitStats.Console
{
    internal class CouplingAnalyser : GitAnalyser
    {
        //What files are always checked in with each other?
        public void Analyse(string gitPath)
        {
            var repo = OpenRepository(gitPath);
            if (repo == null) return;
            System.Console.WriteLine("OK");

            //Ignore merge commits
            var commits = repo.Commits.Where(c => !c.Message.ToLowerInvariant().StartsWith("merge"));
            var commitsProcessed = 0;
            var commitsTotal = commits.Count();

            var allCommitsByFile = new Dictionary<string, List<string>>();
            var allFilesByCommit = new Dictionary<string, List<string>>();

            foreach (var commit in commits)
            {
                commitsProcessed++;
                if (commitsProcessed % 100 == 0) { System.Console.WriteLine($"Progress: {commitsProcessed}/{commitsTotal}"); }
                foreach (var parent in commit.Parents)
                {
                    foreach (var change in repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree))
                    {
                        if (allCommitsByFile.ContainsKey(change.Path))
                        {
                            allCommitsByFile[change.Path].Add(commit.Sha);
                        }
                        else
                        {
                            allCommitsByFile.Add(change.Path, new List<string>() { commit.Sha });
                        }

                        if (allFilesByCommit.ContainsKey(commit.Sha))
                        {
                            allFilesByCommit[commit.Sha].Add(change.Path);
                        }
                        else
                        {
                            allFilesByCommit.Add(commit.Sha, new List<string>() { change.Path });
                        }
                    }
                }
            }

            System.Console.WriteLine("--- Highly Coupled Files ---");

            //for each file:
            //  count the commits then loop through them
            //  lookup the related files in that commit
            //      track a running total of each file committed at the same time
            //we want related files with the same commit count as the file we're tracking
            // i.e. if a.cs was committed 5 times, and b.cs was in 75%+ of those commits, they're tightly coupled.

            var pairScoresByFile = new Dictionary<string, PairStats>();

            foreach (var allCommitsForAFile in allCommitsByFile)
            {
                var pairStats = new PairStats();
                foreach (var commit in allCommitsForAFile.Value)
                {
                    pairStats.CommitCount++;

                    var pairedFiles = allFilesByCommit[commit].Where(path => path != allCommitsForAFile.Key);
                    foreach (var pairedFile in pairedFiles)
                    {
                        if (pairStats.RelatedFileCounts.ContainsKey(pairedFile))
                        {
                            pairStats.RelatedFileCounts[pairedFile]++;
                        }
                        else
                        {
                            pairStats.RelatedFileCounts.Add(pairedFile, 1);
                        }
                    }
                }
                pairScoresByFile.Add(allCommitsForAFile.Key, pairStats);
            }

            foreach (var pair in pairScoresByFile.Where(p => p.Value.TightCouplingCount > 0 && p.Value.CommitCount > 4)
                                                 .OrderByDescending(p => p.Value.CommitCount)
                                                 .OrderByDescending(p => p.Value.TightCouplingCount)
                                                 .Take(10))
            {
                System.Console.WriteLine($"File: {pair.Key} | {pair.Value.CommitCount} commits");
                foreach (var pairedFile in pair.Value.TightlyCoupledFiles.OrderByDescending(f => f.CouplingRatio))
                {
                    System.Console.WriteLine($"          | {pairedFile.CouplingRatio * 100}% - {pairedFile.FileName}");
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

    internal class PairStats
    {
        public int CommitCount { get; set; }
        public Dictionary<string, int> RelatedFileCounts { get; private set; } = new Dictionary<string, int>();
        public int TightCouplingCount
        {
            get
            {
                return RelatedFileCounts.Count(r => ExceedsCouplingThreshold(r.Value));
            }
        }
        public IEnumerable<FileCouplingStats> TightlyCoupledFiles
        {
            get
            {
                return RelatedFileCounts.Where(r => ExceedsCouplingThreshold(r.Value))
                    .Select(r => new FileCouplingStats()
                    {
                        FileName = r.Key,
                        CouplingRatio = (double)r.Value / CommitCount
                    });
            }
        }
        private bool ExceedsCouplingThreshold(int value)
        {
            return 0.75d < value / CommitCount;
        }
    }

    internal class FileCouplingStats
    {
        public double CouplingRatio { get; set; }
        public string FileName { get; set; }
    }
}