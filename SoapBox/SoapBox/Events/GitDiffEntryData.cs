using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "GitDiffEntryData")]
    public class GitDiffEntryData
    {
        [XmlElement(ElementName = "RelativePath")]
        public string RelativePath { get; set; }
        [XmlElement(ElementName = "ChangeType")]
        public string ChangeType { get; set; }
        [XmlElement(ElementName = "FileChangeUri")]
        public string FileChangeUri { get; set; }
        [XmlElement(ElementName = "FileContentUri")]
        public string FileContentUri { get; set; }
    }

}