using SoapBox.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SoapBox
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class MessageService : IMessageService
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        void IMessageService.Notify(string eventXml, string tfsIdentityXml)
        {
            var newEvent = HandleNewPushEvent(eventXml);
            // do something
        }

        public GitPushEvent HandleNewPushEvent(string pushEventXml)
        {
            XmlSerializer serializer = XmlSerializer.FromTypes(new[] { typeof(GitPushEvent) })[0];
            var stringReader = new StringReader(pushEventXml);
            var pushEvent = (GitPushEvent)serializer.Deserialize(stringReader);
            if (PushHasApiChanges(pushEvent))
                Console.WriteLine("Api Change");
            stringReader.Close();
            return pushEvent;
        }

        public bool PushHasApiChanges(GitPushEvent pushEvent)
        {
            return pushEvent.PushNotification.RefUpdateResults.GitPushRefUpdate.ModifiedFiles.GitDiffEntryData
                .Any(gded => pathIsClusterApi(gded.RelativePath));
        }

        private bool pathIsClusterApi(string path)
        {
            var matchString = @"Sdm\.Cluster\..*?\.Api";
            return Regex.Match(path, matchString).Success;
        }
    }
}
