using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "Pusher")]
    public class Pusher
    {
        [XmlAttribute(AttributeName = "identityType")]
        public string IdentityType { get; set; }
        [XmlAttribute(AttributeName = "identifier")]
        public string Identifier { get; set; }
    }
}