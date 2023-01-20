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
using System.Diagnostics.Eventing.Reader;

namespace ModelManager.Tabs
{
	[MemberOrder(99)]
	public class EventsTab : AbstractServiceTab
	{
		public override string Title
		{
			get
			{
				return "Events";
			}
		}

		public Dictionary<string, IEnumerable<string>> DisplayTableExample()
		{
			throw new NotImplementedException();           
		}

		public IEnumerable<string> GetAllEvents()
		{
			// 4800 Workstation Lock
			// 4801 Workstation Unlock
            string eventID = "4800";
            string LogSource = "Security";
            string sQuery = "*[System/EventID=" + eventID + "]";

            var elQuery = new EventLogQuery(LogSource, PathType.LogName, sQuery);
            List<EventRecord> eventList = new List<EventRecord>();
            using (var elReader = new System.Diagnostics.Eventing.Reader.EventLogReader(elQuery))
            {

                EventRecord eventInstance = elReader.ReadEvent();
                try
                {
                    while ((eventInstance = elReader.ReadEvent()) != null)
                    {
                        if (eventInstance.TimeCreated > DateTime.Now.AddDays(-30))
						{
							var description = eventInstance.FormatDescription();
							//Access event properties here:
							//eventInstance.LogName;
							//eventInstance.ProviderName;
							if (description.StartsWith("The workstation was"))
							{
								eventList.Add(eventInstance);
							}
						}
                    }
                }
                finally
                {
                    if (eventInstance != null)
                        eventInstance.Dispose();
                }
            }

            return eventList.Select(e => $"{e.TimeCreated} - {e.FormatDescription().Replace("\n"," - ").Replace("\r","")}");
		}

		
	}
}
