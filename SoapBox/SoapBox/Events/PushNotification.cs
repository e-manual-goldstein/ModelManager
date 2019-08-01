using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "PushNotification")]
    public class PushNotification
    {
        [XmlElement(ElementName = "RepositoryName")]
        public string RepositoryName { get; set; }
        [XmlElement(ElementName = "RepositoryId")]
        public string RepositoryId { get; set; }
        [XmlElement(ElementName = "ProjectReference")]
        public ProjectReference ProjectReference { get; set; }
        [XmlElement(ElementName = "DefaultBranchName")]
        public string DefaultBranchName { get; set; }
        [XmlElement(ElementName = "RemoteUrl")]
        public string RemoteUrl { get; set; }
        [XmlElement(ElementName = "TeamProjectUri")]
        public string TeamProjectUri { get; set; }
        [XmlElement(ElementName = "Pusher")]
        public Pusher Pusher { get; set; }
        [XmlElement(ElementName = "PusherEmail")]
        public string PusherEmail { get; set; }
        [XmlElement(ElementName = "AuthenticatedUserName")]
        public string AuthenticatedUserName { get; set; }
        [XmlElement(ElementName = "UserDisplayName")]
        public string UserDisplayName { get; set; }
        [XmlElement(ElementName = "RefUpdateResults")]
        public RefUpdateResults RefUpdateResults { get; set; }
        [XmlElement(ElementName = "IncludedCommits")]
        public IncludedCommits IncludedCommits { get; set; }
        [XmlElement(ElementName = "PushTime")]
        public string PushTime { get; set; }
        [XmlElement(ElementName = "PushTimeString")]
        public string PushTimeString { get; set; }
        [XmlElement(ElementName = "PushId")]
        public string PushId { get; set; }
        [XmlElement(ElementName = "PushUrl")]
        public string PushUrl { get; set; }
        [XmlElement(ElementName = "PushCommitsUrl")]
        public string PushCommitsUrl { get; set; }
    }
}