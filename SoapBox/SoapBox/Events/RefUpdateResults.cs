using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "RefUpdateResults")]
    public class RefUpdateResults
    {
        [XmlElement(ElementName = "GitPushRefUpdate")]
        public GitPushRefUpdate GitPushRefUpdate { get; set; }
    }

}