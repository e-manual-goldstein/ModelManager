using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "GitPushRefUpdate")]
    public class GitPushRefUpdate
    {
        [XmlElement(ElementName = "CommitDetails")]
        public CommitDetails CommitDetails { get; set; }
        [XmlElement(ElementName = "TotalCommits")]
        public string TotalCommits { get; set; }
        [XmlElement(ElementName = "RefName")]
        public string RefName { get; set; }
        [XmlElement(ElementName = "RefType")]
        public string RefType { get; set; }
        [XmlElement(ElementName = "RawRefName")]
        public string RawRefName { get; set; }
        [XmlElement(ElementName = "RawRefNamePart")]
        public string RawRefNamePart { get; set; }
        [XmlElement(ElementName = "RefUrl")]
        public string RefUrl { get; set; }
        [XmlElement(ElementName = "OldId")]
        public string OldId { get; set; }
        [XmlElement(ElementName = "ShortOldId")]
        public string ShortOldId { get; set; }
        [XmlElement(ElementName = "OldIdUrl")]
        public string OldIdUrl { get; set; }
        [XmlElement(ElementName = "NewId")]
        public string NewId { get; set; }
        [XmlElement(ElementName = "ShortNewId")]
        public string ShortNewId { get; set; }
        [XmlElement(ElementName = "NewIdUrl")]
        public string NewIdUrl { get; set; }
        [XmlElement(ElementName = "AffectedFolders")]
        public AffectedFolders AffectedFolders { get; set; }
        [XmlElement(ElementName = "HasMoreFolders")]
        public string HasMoreFolders { get; set; }
        [XmlElement(ElementName = "ModifiedFiles")]
        public ModifiedFiles ModifiedFiles { get; set; }
        [XmlElement(ElementName = "HasMoreFiles")]
        public string HasMoreFiles { get; set; }
    }
}