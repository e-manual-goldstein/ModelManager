using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "ModifiedFiles")]
    public class ModifiedFiles
    {
        [XmlElement(ElementName = "GitDiffEntryData")]
        public List<GitDiffEntryData> GitDiffEntryData { get; set; }
    }
}