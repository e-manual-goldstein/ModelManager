using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ModelManager.Core;
using ModelManager.Types;
using System.Threading;

namespace ModelManager.Tabs
{
	[MemberOrder(99)]
	public class ExampleTab : AbstractServiceTab
	{
		public override string Title
		{
			get
			{
				return "Example";
			}
		}

		public Dictionary<string, IEnumerable<string>> DisplayTableExample()
		{
            return randomTable(5, 15);
		}

		public IEnumerable<string> DisplayListExample()
		{
			return randomList(5);
		}

		public void DisplayErrorExample()
		{
			throw new Exception("This is an example of an error");
		}

		public List<string> DisplayOutputWithParameters([Mandatory]string inputString, [Mandatory]int numberField, string anotherInputStringWithAVeryLongName)
		{
			return new List<string>() { inputString, numberField.ToString(), anotherInputStringWithAVeryLongName };
		}

		//public void DisplayBranches()
		//{
		//	var section = ConfigurationManager.GetSection("LocalBranches") as NameValueCollection;
		//	var sdmDevBranch = section["SDM_DEV"];
			
		//}

		//public void AddNewLocalBranch(string branchName, string branchPath)
		//{
		//	TFUtils.AddNewLocalBranch(branchName, branchPath);
		//}

		#region DEBUG

		public void DisplayDifferences(bool includeAll)
		{
			
		}

		private void updateMargin(Thickness newMargin)
		{
			var tabs = App.Manager.TabManager.OutputTabs;
			foreach (var tab in tabs)
			{
				tab.TabItemControl.Padding = newMargin;
			}
		}

        private Random random = new Random();

        private IEnumerable<string> randomList(int rowCount)
        {
            var randomList = new List<string>();
            for (int i = 0; i < rowCount; i++)
            {
                randomList.Add(random.Next().ToString());
            }
            return randomList;
        }

        private Dictionary<string, IEnumerable<string>> randomTable(int rowCount, int columnCount)
        {
            var dictionary = new Dictionary<string, IEnumerable<string>>();
            for (int i = 0; i < columnCount; i++)
            {
                var columnName = "Column_" + i;
                var list = randomList(rowCount);
                dictionary.Add(columnName, list);
            }
            return dictionary;
        }

        #endregion

		
	}
}
