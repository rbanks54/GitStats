# GitStats
View various statistics on your team's git repository usage to identify behaviour patterns and problem areas and help teams improve

#### Contributing
This code was written in a short space of time for a talk at the Sydney Alt.Net user group and it has a lot of scope for growth. It'll improve over time, but for now it's pretty rough.

## Usages
GitStats is a simple console application at this point in time. Run it and you'll be prompted for a path to a local repository and the analysis you want to run.  The options are:

### Sentiment Analysis
Do the commit messages by the team indicate a general level of happiness with the code base, or are they getting grumpy or frustrated.

Recent messages from the git commit log are sent to [Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/text-analytics/) for analysis, 
and the results are shown on screen.

For now, you will need to enter your Cognitive Services API key in the app.config file.

Note: The free tier for text analytics only allows 5,000 transactions per month, across both sentiment and topic analysis. You might want to restrict your usage on this one if you don't have a paid tier.

### Key Phrase Analysis

What phrases are the most common across your recent commit messages? Are they related to the same subject or scattered across many areas?
Are there phrases in the commit messages that you don't expect?

Much like sentiment analysis, recent messages from your git commit log are sent to [Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/text-analytics/) for analysis.
The code limits the analysis to the last 100 messages but it's simple to change.

For now, you will need to enter your Cognitive Services API key in the app.config file.

Note: The free tier for text analytics only allows 5,000 transactions per month, across both sentiment and topic analysis. You might want to restrict your usage on this one if you don't have a paid tier.

### BugSpots
Which files in your code base are most prone to bugs?

We determine this by looking at the number of commits for a file containing words that indicate a bug fix was made, and looking at the age of those fixes.
Files with a high number of recent bug fix commits need special attention as they most likely are the hardest to work on at the moment.
Files that had a high number of bug fix commits a long time ago, and not many in recent times are likely to be healthy now and should have a lower score.

This analysis is based off [Google's Bug Prediction approach](http://google-engtools.blogspot.com.au/2011/12/bug-prediction-at-google.html), which is well worth a read if you've not seen it before.

### Commit Coupling
(I tend to think of these as cluster-bomb commits).
Do you have sets of files that are always coupled together in commits. It might indicate a high level of copy/paste development, 
or that system behaviours are spread across too many files and your logic is not well organised or compartmentalised.

You might also find that you have application code and related test code always being checked in together. This might seem OK, but if every change in a file requires a matching change in the test code it could indicate that your test code is too tightly coupled to the implementation instead of the behaviour.

### Impact (Cognitive Load)
The larger the commit, the harder it is to understand what the change is actually doing.
The larger the commit, the more likely it is for problems to sneak through the pull request process.

The folks at [GitPrime blogged about their approach to measuring the impact of a change](https://blog.gitprime.com/impact-a-better-way-to-measure-codebase-change) and this analysis borrows their idea, though lacks some of the measures they include.

With this analysis you can see:

* the highest impact commits
* the average impact by day of week (e.g. which day of the week are we at our worst?)
* which teams members have the highest average impact (so we can help improve our behaviours)

### Author Statistics
Files with a high number of authors can be problematic. (I tend to think of this as crowd-sourced code).

They can be hot spots for merge conflicts, often leading to unintended bugs.
They can indicate that there is too much logic in a single location or that a "god class" exists in the codebase.
They could indicate that there is some poor design, requiring all changes to have related changes in a common file somewhere.

Regardless, if you have a high number of authors in a file, it's worth looking in to the reasons why.

## Stats we don't track
GitHub has some good statistics available under the Insights drop down. At this point in time, there are no plans to replicate those.
