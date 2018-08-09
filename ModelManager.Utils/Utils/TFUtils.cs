using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModelManager.Utils
{
	public static class TFUtils
	{
        public static void LookupBranchesOnline() //Dictionary<string, string>
        {
            Uri tfsUri = new Uri(AppUtils.GetAppSetting<string>("TFPath"));
            //var vssCredentials = new VssCredentials("", );
            NetworkCredential netCred = new NetworkCredential(@"DOMAIN\user.name", @"Password1");
            var winCred = new Microsoft.VisualStudio.Services.Common.WindowsCredential(netCred);
            VssCredentials vssCred = new VssClientCredentials(winCred);

            // Bonus - if you want to remain in control when
            // credentials are wrong, set 'CredentialPromptType.DoNotPrompt'.
            // This will thrown exception 'TFS30063' (without hanging!).
            // Then you can handle accordingly.
            vssCred.PromptType = CredentialPromptType.DoNotPrompt;

            // Now you can connect to TFS passing Uri and VssCredentials instances as parameters
            
            var tfsTeamProjectCollection = new TfsTeamProjectCollection(tfsUri, vssCred);

            // Finally, to make sure you are authenticated...
            tfsTeamProjectCollection.EnsureAuthenticated();

            var tfs = new TfsTeamProjectCollection(tfsUri);
            var identity = tfs.AuthorizedIdentity;
            tfs.Authenticate();
            var versionControlServer = tfs.GetService<VersionControlServer>();
            var workspaces = versionControlServer.QueryWorkspaces(null, null, null, WorkspacePermissions.NoneOrNotSupported);
            foreach (var ws in workspaces)
            {
                string comment = ws.Comment;
            }
        }

		public static void AddNewLocalBranch(string branchName, string branchPath)
		{
			var xmlDoc = new XmlDocument();
			xmlDoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

			// create new node <add key="Region" value="Canterbury" />
			var nodeRegion = xmlDoc.CreateElement("add");
			nodeRegion.SetAttribute("key", branchName);
			nodeRegion.SetAttribute("value", branchPath);

			xmlDoc.SelectSingleNode("//LocalBranches").AppendChild(nodeRegion);
			xmlDoc.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

			ConfigurationManager.RefreshSection("LocalBranches");
		}

        private static Changeset GetChangeset(Uri serverUri, int changesetId)
        {
            var tfs = new TfsTeamProjectCollection(serverUri);
            var svc = tfs.GetService<VersionControlServer>();
            var changeset = svc.GetChangeset(changesetId);
            return changeset;
        }
    }
}
