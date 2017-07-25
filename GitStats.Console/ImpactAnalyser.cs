using EnsureThat;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitStats.Console
{
    internal class ImpactAnalyser : GitAnalyser
    {
        //See https://blog.gitprime.com/impact-a-better-way-to-measure-codebase-change
        //We'll use 3 factors
        // * The amount of code in the change
        // * What percentage of the work is edits to old code
        // * The number of files affected

        public void Analyse(string gitPath)
        {
            var repo = OpenRepository(gitPath);
            if (repo == null) return;
            System.Console.WriteLine("OK");

            //Ignore merge commits
            var commits = repo.Commits.Where(c => !c.Message.ToLowerInvariant().StartsWith("merge"));
            var commitsProcessed = 0;
            var commitsTotal = commits.Count();
            var tree = commits.First();

            var patchImpact = new Dictionary<string, ImpactStats>();

            foreach (var commit in commits)
            {
                commitsProcessed++;
                if (commitsProcessed % 100 == 0) { System.Console.WriteLine($"Progress: {commitsProcessed}/{commitsTotal}"); }
                foreach (var parent in commit.Parents)
                {
                    var impact = new ImpactStats() {
                        Who = commit.Committer.Name,
                        When = commit.Committer.When
                    };
                    foreach (var change in repo.Diff.Compare<Patch>(parent.Tree,commit.Tree))
                    {
                        switch (change.Status)
                        {
                            case ChangeKind.Modified:
                            case ChangeKind.Conflicted:
                                impact.EditedLines += change.LinesAdded + change.LinesDeleted;
                                impact.FilesChanged++;
                                break;
                            case ChangeKind.Added:
                                impact.AddedLines += change.LinesAdded;
                                impact.FilesAdded++;
                                break;
                            case ChangeKind.Deleted:
                                impact.RemovedLines += change.LinesDeleted;
                                impact.FilesRemoved++;
                                break;
                            case ChangeKind.Renamed:
                                break;
                            default:
                                System.Console.WriteLine("Status: " + change.Status.ToString());
                                break;
                        }
                    }
                    if (patchImpact.ContainsKey(commit.Sha))
                    {
                        //Mulitple parents, and not a merge commit - probably a conflict resolution. Double counting is OK.
                        patchImpact[commit.Sha].AddedLines += impact.AddedLines;
                        patchImpact[commit.Sha].RemovedLines += impact.RemovedLines;
                        patchImpact[commit.Sha].EditedLines += impact.EditedLines;
                        patchImpact[commit.Sha].FilesChanged += impact.FilesChanged;
                    }
                    else
                    {
                        patchImpact.Add(commit.Sha, impact);
                    }
                }
            }

            System.Console.WriteLine("--- High Impact Commits ---");

            foreach (var patch in patchImpact.OrderByDescending(pair => pair.Value.ImpactScore).Take(5))
            {
                System.Console.WriteLine($"Id: {patch.Key} | {patch.Value.ImpactScore} ({patch.Value.Who})");
                System.Console.WriteLine($"          | Files: +{patch.Value.FilesAdded}, -{patch.Value.FilesRemoved}, ~{patch.Value.FilesChanged}");
                System.Console.WriteLine($"          | Lines: +{patch.Value.AddedLines}, -{patch.Value.RemovedLines}, ~{patch.Value.EditedLines}");
            }

            var badDays = from p in patchImpact
                          group p.Value by p.Value.When.DayOfWeek into g
                          orderby g.Key
                          select new {
                              dow = g.Key.ToString(),
                              averageScore = g.Average(i => i.ImpactScore),
                              commitCount = g.Count()
                          };

            System.Console.WriteLine("--- Day Ratings ---");
            foreach (var dayData in badDays)
            {
                System.Console.WriteLine($"Day of Week: {dayData.dow} | Average: {dayData.averageScore} ({dayData.commitCount})");
            }

            var peopleRatings = from p in patchImpact
                          group p.Value by p.Value.Who into g
                          select new
                          {
                              who = g.Key,
                              averageScore = g.Average(i => i.ImpactScore),
                              commitCount = g.Count()
                          };

            System.Console.WriteLine("--- People Ratings ---");
            foreach (var personRating in peopleRatings.OrderByDescending(r => r.averageScore))
            {
                System.Console.WriteLine($"Who: {personRating.who} | Average: {personRating.averageScore} ({personRating.commitCount})");
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

    internal class ImpactStats
    {
        public string Who { get; set; }
        public DateTimeOffset When { get; set; }
        public int AddedLines { get; set; }
        public int RemovedLines { get; set; }
        public int EditedLines { get; set; }
        public int FilesChanged { get; set; }
        public int FilesAdded { get; set; }
        public int FilesRemoved { get; set; }
        public double ImpactScore { get
            {
                int totalLines = (EditedLines + AddedLines + RemovedLines);
                int interestingLines = EditedLines + AddedLines;
                double oldCodeWeighting = totalLines == 0 ? 0 : EditedLines / totalLines;
                double baseScore = 10d * FilesChanged
                                 + 3d * FilesAdded
                                 + FilesRemoved
                                 + interestingLines;
                return baseScore + (baseScore * oldCodeWeighting);
            }
        }
    }

}