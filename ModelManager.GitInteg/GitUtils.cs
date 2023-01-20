using LibGit2Sharp;
using ModelManager.Types;
using ModelManager.Utils;
using StaticCodeAnalysis;
using StaticCodeAnalysis.CodeComparer;
using StaticCodeAnalysis.CodeStructures;
using StaticCodeAnalysis.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModelManager.GitInteg
{
	public class GitUtils
	{
		static string _linesRemovedPattern = @"\-((?'LineNumberRemoved'[0-9]*)\,)??(?'LinesRemoved'[0-9]+)";
		static string _linesAddedPattern = @"\+((?'LineNumberAdded'[0-9]*)\,)??(?'LinesAdded'[0-9]+)";
		public static string RepoPath = LocalSettings.Instance().RepoPath;

		public static IEnumerable<CodeFileComparer> ReadRepository()
		{
			using (var repo = new Repository(RepoPath))
			{
				var masterBranches = repo.Branches.Where(c => c.FriendlyName.Contains("master")).ToList();
				var latestCommit = masterBranches.FirstOrDefault().Commits.Skip(0).First();
				var previousCommit = masterBranches.FirstOrDefault().Commits.Skip(1).First();


				var treeChanges = repo.Diff.Compare<TreeChanges>(previousCommit.Tree, latestCommit.Tree);
				var modifiedItems = treeChanges.Modified;
				foreach (var modifiedItem in modifiedItems)
				{
					if (IsCodeFile(modifiedItem.Path))
					{
						var patchChanges = repo.Diff.Compare<Patch>(previousCommit.Tree, latestCommit.Tree, new List<string>() { modifiedItem.Path });
						var changeBlocks = getChangeBlocks(patchChanges);
						var previousFileContents = getFileBySha(repo, modifiedItem.Path, previousCommit.Sha);
						var newFileContents = getFileBySha(repo, modifiedItem.Path, latestCommit.Sha);
						var previousCodeFile = CodeUtils.CreateCodeFileFromContents(previousFileContents, "source", modifiedItem.Path);
						var newCodeFile = CodeUtils.CreateCodeFileFromContents(newFileContents, "target", modifiedItem.Path);
						yield return new CodeFileComparer(previousCodeFile, newCodeFile, null);
					}
				}
			}
		}
		public static List<string> ListBranches(string filter, bool regEx)
		{
			using (var repo = new Repository(RepoPath))
			{
				var branches = repo.Branches;
				if (!string.IsNullOrEmpty(filter))
					return branches.Where(b => applyFilter(b, filter, regEx)).Select(b => b.FriendlyName).ToList();
				return branches.Select(b => b.FriendlyName).ToList();
			}
		}

		private static bool applyFilter(Branch branch, string filter, bool regEx)
		{
			if (regEx)
				return Regex.Match(branch.FriendlyName, filter).Success;
			return branch.FriendlyName.Contains(filter);
		}

		public static CodeFileComparer GetTwoFiles()
		{
			var baseRepositoryPath = RepoPath;

			var sourceRepoOpts = configureTemporaryRepository(baseRepositoryPath, "source");
			var targetRepoOpts = configureTemporaryRepository(baseRepositoryPath, "target");


			using (var repo = new Repository(RepoPath))
			using (var sourceRepo = new Repository(repo.Info.Path, sourceRepoOpts))
			using (var targetRepo = new Repository(repo.Info.Path, targetRepoOpts))
			{
				var masterBranches = repo.Branches.Where(c => c.FriendlyName.Contains("master")).ToList();
				var masterBranch = masterBranches.FirstOrDefault();
				var latestCommit = masterBranch.Commits.Skip(0).First();
				var previousCommit = masterBranch.Commits.Skip(1).First();

				var treeChanges = repo.Diff.Compare<TreeChanges>(previousCommit.Tree, latestCommit.Tree);
				var modifiedItems = treeChanges.Modified;
				foreach (var modifiedItem in modifiedItems)
				{
					if (IsCodeFile(modifiedItem.Path))
					{
						var sourceFileContents = getFileBySha(sourceRepo, modifiedItem.Path, previousCommit.Sha);
						var targetFileContents = getFileBySha(targetRepo, modifiedItem.Path, latestCommit.Sha);
						var sourceCodeFile = CodeUtils.CreateCodeFileFromContents(sourceFileContents, "source", modifiedItem.Path);
						var targetCodeFile = CodeUtils.CreateCodeFileFromContents(targetFileContents, "target", modifiedItem.Path);
						return new CodeFileComparer(sourceCodeFile, targetCodeFile, null);
					}
				}
				return null;
			}
		}

		private static RepositoryOptions configureTemporaryRepository(string baseRepositoryPath, string repoName)
		{
			removeTemporaryRepository(baseRepositoryPath, repoName);
			var tempPath = Path.Combine(Path.GetDirectoryName(baseRepositoryPath), repoName);
			string tempIndex = Path.Combine(tempPath, "tmp_idx");
			Directory.CreateDirectory(tempPath);

			var repoOpts = new RepositoryOptions
			{
				WorkingDirectoryPath = tempPath,
				IndexPath = tempIndex
			};
			return repoOpts;
		}

		private static void removeTemporaryRepository(string baseRepositoryPath, string repoName)
		{
			var tempPath = Path.Combine(Path.GetDirectoryName(baseRepositoryPath), repoName);
			if (File.Exists(Path.Combine(tempPath, "tmp_idx")))
				Directory.Delete(tempPath, true);
		}

		public static List<string> FindCommitsWithChanges(string repoName, DateTime fromDate, int? daysToRetrospect, int resultLimit = 20)
		{
			var baseRepositoryPath = RepoPath;
			var commitIds = new List<string>();
			if (daysToRetrospect.HasValue)
				fromDate = DateTime.Today.AddDays(0 - daysToRetrospect.Value);
			var commitList = new List<string>();
			using (var repo = new Repository(baseRepositoryPath))
			{
				var branches = repo.Branches.Where(c => repoName == null || c.FriendlyName.ToUpper().Contains(repoName.ToUpper()));
				foreach (var branch in branches)
				{
					var commits = branch.Commits.Where(c => c.Committer.When >= fromDate && !c.MessageShort.TrimStart(' ').StartsWith("Merge branch"))
						.ToList();
					foreach (var commit in commits)
					{
						if (commitIds.Any(c => c == commit.Sha))
							continue;
						commitIds.Add(commit.Sha);
						getChangesFromCommit(repo, branch, commit, baseRepositoryPath, commitList);
					}
				}
			}
			return commitList;
		}

		private static void getChangesFromCommit(Repository repo, Branch branch, Commit commit, string baseRepositoryPath, List<string> commitList)
		{
			if (!commit.Parents.Any())
				return;
			var commitChanges = repo.Diff.Compare<TreeChanges>(commit.Tree, commit.Parents.First().Tree).Modified;
			var sb = new StringBuilder("");
			if (commitChanges.Any(c => fileFilterMatch(c)))
			{
				sb.AppendLine("Branch: " + branch.FriendlyName);
				sb.AppendLine("Message: " + commit.MessageShort);
				sb.AppendLine(GetShortId(commit.Sha) + " by " + commit.Committer.Name + ", " + commit.Committer.When);
				var filteredFiles = commitChanges.Where(cc => fileFilterMatch(cc)).Select(c => c.Path);
				if (filteredFiles.Count() <= 20)
				{
					sb.AppendLine("Changes: ");
					var codeFiles = filteredFiles.Where(c => IsCodeFile(c));
					var differenceReports = generateDifferenceReports(codeFiles, repo, commit, baseRepositoryPath);
					foreach (var differenceReport in differenceReports)
					{
						sb.AppendLine(differenceReport);
					}
					removeTemporaryRepository(baseRepositoryPath, "source");
					removeTemporaryRepository(baseRepositoryPath, "target");
				}
				else
					sb.AppendLine("Changed files: " + filteredFiles.Count());
				sb.AppendLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
			}
			commitList.Add(sb.ToString());
		}

		private static string generateDifferenceReport(string filePath, Repository repo, Commit commit, string baseRepositoryPath)
		{
			var sourceRepoOpts = configureTemporaryRepository(baseRepositoryPath, "source");
			var targetRepoOpts = configureTemporaryRepository(baseRepositoryPath, "target");

			using (var sourceRepo = new Repository(repo.Info.Path, sourceRepoOpts))
			using (var targetRepo = new Repository(repo.Info.Path, targetRepoOpts))
			{
				var parentCommit = commit.Parents.First();
				var sourceFileContents = getFileBySha(sourceRepo, filePath, parentCommit.Sha);
				var targetFileContents = getFileBySha(targetRepo, filePath, commit.Sha);
				return CodeUtils.DescribeDifferencesFromContents(sourceFileContents, targetFileContents, filePath);
			}
		}
		private static IEnumerable<string> generateDifferenceReports(IEnumerable<string> filePaths, Repository repo, Commit commit, string baseRepositoryPath)
		{
			var sourceRepoOpts = configureTemporaryRepository(baseRepositoryPath, "source");
			var targetRepoOpts = configureTemporaryRepository(baseRepositoryPath, "target");

			using (var sourceRepo = new Repository(repo.Info.Path, sourceRepoOpts))
			using (var targetRepo = new Repository(repo.Info.Path, targetRepoOpts))
			{
				var parentCommit = commit.Parents.First();
				var sourceFileContents = getFilesBySha(sourceRepo, filePaths, parentCommit.Sha);
				var targetFileContents = getFilesBySha(targetRepo, filePaths, commit.Sha);
				foreach (var filePath in filePaths)
				{
					yield return CodeUtils.DescribeDifferencesFromContents(sourceFileContents[filePath], targetFileContents[filePath], filePath);
				}
			}
		}

		private static bool fileFilterMatch(TreeEntryChanges entry)
		{
			var filterFilePath = LocalSettings.Instance().GitFilterFilePath;
			if (File.Exists(filterFilePath))
			{
				var regexFilters = File.ReadAllLines(filterFilePath);
				foreach (var regexPattern in regexFilters)
				{
					if (!Regex.Match(entry.Path, regexPattern, RegexOptions.IgnoreCase).Success)
						return false;
				}
			}
			return true;
		}

		public static string GetShortId(string sha)
		{
			return sha.Substring(0, 6);
		}

		private static string getFileBySha(Repository repo, string path, string sha)
		{
			repo.CheckoutPaths(sha, new[] { path });
			return File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, path));
		}

		private static IDictionary<string, string> getFilesBySha(Repository repo, IEnumerable<string> filePaths, string sha)
		{
			var dictionary = new Dictionary<string, string>();
			repo.CheckoutPaths(sha, filePaths);
			foreach (var filePath in filePaths)
			{
				dictionary.Add(filePath, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, filePath)));
			}
			return dictionary;
		}

		private static List<ChangeBlock> getChangeBlocks(string patchChangeContent)
		{
			var changeBlocks = new List<ChangeBlock>();
			var changeSegmentPattern = @"\@\@\s" + _linesRemovedPattern + @"\s" + _linesAddedPattern + @"\s\@\@";
			var codeMatches = Regex.Matches(patchChangeContent, changeSegmentPattern, RegexOptions.Singleline);
			foreach (Match codeMatch in codeMatches)
			{
				var lineNumber = codeMatch.Groups["LineNumberAdded"].Value;
				if (string.IsNullOrWhiteSpace(lineNumber))
					lineNumber = "0";
				var linesAdded = codeMatch.Groups["LinesAdded"].Value;
				var linesRemoved = codeMatch.Groups["LinesRemoved"].Value;
				var changeBlock = new ChangeBlock(lineNumber, linesAdded, linesRemoved);
				changeBlocks.Add(changeBlock);
			}
			for (int i = 0; i < changeBlocks.Count; i++)
			{
				var thisBlock = changeBlocks[i];
				var thisBlockPattern = thisBlock.MatchPattern;
				var nextBlockStartPattern = changeBlocks.Count - 1 > i ? changeBlocks[i + 1].MatchPattern : "$";
				var blockContent = Regex.Match(patchChangeContent, thisBlock.MatchPattern + "(?'ChangeContent'.*)" + nextBlockStartPattern, RegexOptions.Singleline);
				thisBlock.Content = blockContent.Groups["ChangeContent"].Value;
			}
			return changeBlocks;
		}

		public static bool IsCodeFile(string filePath)
		{
			return Regex.Match(filePath, @".*\.cs$").Success;
		}

		public static Dictionary<string, string> SplitContent(string combinedContent)
		{
			var dictionary = new Dictionary<string, string>();
			var oldContent = extractContent(combinedContent, Discriminator.Added);
			var newContent = extractContent(combinedContent, Discriminator.Removed);
			dictionary.Add("OldContent", oldContent);
			dictionary.Add("NewContent", newContent);
			return dictionary;
		}

		private static string extractContent(string combinedContent, Discriminator discriminator)
		{
			var sign = discriminator == Discriminator.Added ? "+" : "-";
			var antiSign = discriminator == Discriminator.Added ? "-" : "+";
			var extractLinePattern = @"(\r\n|^)(?'ExtractLine'\" + sign + ".*?(\r\n|$))";
			var extractLineMatches = Regex.Matches(combinedContent, extractLinePattern, RegexOptions.Multiline);
			var trimLinePattern = @"(\r\n|^)(?'TrimLine'\" + antiSign + "(?'RestOfLine'.*?(\r\n|$)))";
			var newContent = combinedContent;
			foreach (Match lineMatch in extractLineMatches)
			{
				var extractLine = lineMatch.Groups["ExtractLine"].Value;
				newContent = newContent.Replace(extractLine, "");
			}
			var trimLineMatches = Regex.Matches(newContent, trimLinePattern, RegexOptions.Multiline);
			foreach (Match trimMatch in trimLineMatches)
			{
				var trimLine = trimMatch.Groups["TrimLine"].Value;
				var restOfLine = trimMatch.Groups["RestOfLine"].Value;
				newContent = newContent.Replace(trimLine, restOfLine);
			}
			return newContent;
		}

		public static Dictionary<string, IEnumerable<string>> FindCommitsByUser(string userName)
		{
			var baseRepositoryPath = RepoPath;
			var outputTable = new Dictionary<string, IEnumerable<string>>();
			//List<string> commitList;
			using (var repo = new Repository(baseRepositoryPath))
			{
				var commits = repo.Commits;
				var userCommits = commits.Where(c => c.Author.Name.Contains(userName));
				var shaIds = userCommits.Select(c => c.Sha).ToList();
				var authors = userCommits.Select(c => c.Author.Name).ToList();
				var messages = userCommits.Select(c => c.Message).ToList();
				var commitDates = userCommits.Select(c => c.Committer.When.Date.ToString()).ToList();
				outputTable.Add("ShaId", shaIds);
				outputTable.Add("Author", authors);
				outputTable.Add("Message", messages);
				outputTable.Add("CommitDate", commitDates);
				//commitList = userCommits.Select(c => c.Sha + "\t" + c.Author + "\t" + c.MessageShort).ToList();
			}
			return outputTable;
		}

		public static Dictionary<string, IEnumerable<string>> GetPullRequestReport()
		{
			var baseRepositoryPath = RepoPath;
			var outputTable = new Dictionary<string, IEnumerable<string>>();
			var reportTable = new Dictionary<DateTime, List<Commit>>();
			var falseTable = new Dictionary<DateTime, int>();
			//List<string> commitList;
			using (var repo = new Repository(baseRepositoryPath))
			{
				var developBranch = repo.Branches.First(b => b.FriendlyName == "develop");
				var pullRequests = developBranch.Commits;//.Where(c => c.Message.Contains(" PR "));
				foreach (var pullRequest in pullRequests)
				{
					var fullMatch = Regex.Match(pullRequest.Message, @"PR [0-9]*\:").Success;
					if (fullMatch)
					{
						var date = pullRequest.Committer.When.Date;
						if (reportTable.ContainsKey(date))
							reportTable[date].Add(pullRequest);
						else
							reportTable[date] = new List<Commit> { pullRequest };
					}
					else
					{
						var date = pullRequest.Committer.When.Date;
						if (falseTable.ContainsKey(date))
							falseTable[date]++;
						else
							falseTable[date] = 1;
					}
				}
				var orderedTable = reportTable.OrderBy(c => c.Key);
				var dates = orderedTable.Select(r => r.Key.ToString("dd/MM/yyyy"));
				var counts = orderedTable.Select(r => r.Value.Count().ToString());
				outputTable.Add("Date", dates);
				outputTable.Add("Count", counts);
			}
			return outputTable;
		}

		public static IEnumerable<LogEntry> GetFileHistory(string filePath)
		{
			var baseRepositoryPath = RepoPath.Replace(@"\",@"\\");
			var match = Regex.Match(filePath, @"" + baseRepositoryPath + @"(?'RelativePath'.*)");
			var relativePath = match.Groups["RelativePath"].Value.Replace(@"\",@"/") ;
			var outputTable = new Dictionary<string, IEnumerable<string>>();
			//List<string> commitList;
			using (var repo = new Repository(baseRepositoryPath))
			{
				var fileHistory = repo.Commits.QueryBy(relativePath);
				var recentFileHistory = fileHistory;//.Where(fh => fh.Commit.Author.When > DateTime.Now.AddDays(-1));
				foreach (var item in recentFileHistory)
				{
					var logEntry = item.Commit;
				}
				return fileHistory;
			}

		}
	}
}
