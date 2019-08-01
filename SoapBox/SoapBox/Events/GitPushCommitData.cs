using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "GitPushCommitData")]
    public class GitPushCommitData
    {
        [XmlElement(ElementName = "Parents")]
        public Parents Parents { get; set; }
        [XmlElement(ElementName = "CommitTime")]
        public string CommitTime { get; set; }
        [XmlElement(ElementName = "CommitTimeString")]
        public string CommitTimeString { get; set; }
        [XmlElement(ElementName = "Author")]
        public string Author { get; set; }
        [XmlElement(ElementName = "AuthorTime")]
        public string AuthorTime { get; set; }
        [XmlElement(ElementName = "AuthorTimeString")]
        public string AuthorTimeString { get; set; }
        [XmlElement(ElementName = "Committer")]
        public string Committer { get; set; }
        [XmlElement(ElementName = "Comment")]
        public string Comment { get; set; }
        [XmlElement(ElementName = "ShortComment")]
        public string ShortComment { get; set; }
        [XmlElement(ElementName = "Id")]
        public string Id { get; set; }
        [XmlElement(ElementName = "ShortId")]
        public string ShortId { get; set; }
        [XmlElement(ElementName = "IdUrl")]
        public string IdUrl { get; set; }
        [XmlElement(ElementName = "CommitDiffs")]
        public CommitDiffs CommitDiffs { get; set; }
        [XmlElement(ElementName = "HasMoreDiffs")]
        public string HasMoreDiffs { get; set; }
    }
}