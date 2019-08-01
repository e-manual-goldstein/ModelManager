using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{
    [XmlRoot(ElementName = "GitPushEvent")]
    public class GitPushEvent
    {
        [XmlElement(ElementName = "PushNotification")]
        public PushNotification PushNotification { get; set; }
        [XmlElement(ElementName = "RepositoryUri")]
        public string RepositoryUri { get; set; }
        [XmlElement(ElementName = "TimeZoneOffset")]
        public string TimeZoneOffset { get; set; }
        [XmlElement(ElementName = "TimeZoneName")]
        public string TimeZoneName { get; set; }
        [XmlElement(ElementName = "Title")]
        public string Title { get; set; }
        [XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsd { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
    }

}