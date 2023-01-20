using ModelManager.Core;
using ModelManager.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Tabs
{
	[MemberOrder(1)]
	public class GitTab : AbstractServiceTab
	{
		public override string Title
		{
			get
			{
				return "Git";
			}
		}

		public Dictionary<string, IEnumerable<string>> ReadGitRepository()
		{
            var dictionary = new Dictionary<string, IEnumerable<string>>();
            //var comparers = GitUtils.ReadRepository().ToList();
			var comparer = GitUtils.GetTwoFiles();
			var differences = comparer.GetDifferences();
            dictionary.Add("Element", differences.Select(d => d.ElementName).ToList());
            dictionary.Add("Feature", differences.Select(d => d.Feature).ToList());
            dictionary.Add("Source Value", differences.Select(d => d.SourceValue).ToList());
            dictionary.Add("Target Value", differences.Select(d => d.TargetValue).ToList());
            return dictionary;
		}

		public List<string> FindRecentChanges()
		{
			var fromDate = DateTime.Today.AddDays(-30);
			var commits =  GitUtils.FindCommitsWithChanges("master",fromDate,null);
			return commits;
		}

		public List<string> FindChangesWithFilter(string repoName, DateTime? fromDate, int? daysToRetrospect)
		{
			if (fromDate.HasValue && daysToRetrospect.HasValue)
				throw new ArgumentException("Must choose 'From Date' OR 'Days To Retrospect', cannot filter by both");
			var commits = GitUtils.FindCommitsWithChanges(repoName, fromDate ?? DateTime.MinValue, daysToRetrospect);
			return commits;
		}

		public Dictionary<string, IEnumerable<string>> FindCommitsByUser(string username)
		{
			return GitUtils.FindCommitsByUser(username);
		}

		public Dictionary<string, IEnumerable<string>> GetPullRequestReport()
		{
			return GitUtils.GetPullRequestReport();
		}

		public List<string> ListBranches(string filter, bool useRegex)
		{
			return GitUtils.ListBranches(filter, useRegex);
		}

		public void GetFileHistory(string filePath)
		{
			GitUtils.GetFileHistory(filePath);
		}
	}
}
