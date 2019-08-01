using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "CommitDetails")]
    public class CommitDetails
    {
        [XmlElement(ElementName = "GitPushCommitData")]
        public List<GitPushCommitData> GitPushCommitData { get; set; }
    }
}