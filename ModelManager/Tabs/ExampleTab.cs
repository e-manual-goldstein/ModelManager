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
using ModelManager.Tabs.Outputs;
using Microsoft.Extensions.Logging;

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

		public string LongRunningTaskExample(int wait, int loops)
		{
			using (var progressUpdater = new ProgressUpdater(this))
			{ 
				var logger = LoggerProvider.CreateLogger("Example Tab");
				for (int i = 0; i < loops; i++)
				{
					logger.LogInformation($"Waited {i} seconds");
                    Task.Delay(wait).Wait();
					progressUpdater.UpdateProgress(i + 1, loops);
				}
				return "Complete";
			}
		}
		public void DisplayErrorExample()
		{
			throw new Exception("This is an example of an error");
		}

		public List<string> DisplayOutputWithParameters([Mandatory]string inputString, [Mandatory]int numberField, string anotherInputStringWithAVeryLongName)
		{
			return new List<string>() { inputString, numberField.ToString(), anotherInputStringWithAVeryLongName };
		}

		public ListOutput GetListExample()
		{
			var output = new ListOutput(randomList(5));
            output.ContentActions.Add("Example", ExampleOutputAction);
			return output;
        }
		//}
		public ListOutput ExampleOutputAction(IEnumerable<string> inputs)
		{
			var outputList = new List<string>();
			foreach (var input in inputs)
			{
				outputList.Add(input.Substring(0, input.Length - 1));
			}
			var output = new ListOutput(outputList);
			output.ContentActions.Add("Example", ExampleOutputAction);
			return output;
        }

		#region DEBUG

		public void DisplayDifferences(bool includeAll)
		{
			
		}

		//private void updateMargin(Thickness newMargin)
		//{
			//var tabs = App.Manager.TabManager.OutputTabs;
			//foreach (var tab in tabs)
			//{
			//	tab.TabItemControl.Padding = newMargin;
			//}
		//}

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
