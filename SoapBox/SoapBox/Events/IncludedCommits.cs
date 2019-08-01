using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{

    [XmlRoot(ElementName = "IncludedCommits")]
    public class IncludedCommits
    {
        [XmlElement(ElementName = "string")]
        public List<string> String { get; set; }
    }
}