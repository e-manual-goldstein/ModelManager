﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace SoapBox.Events
{

    [XmlRoot(ElementName = "AffectedFolders")]
    public class AffectedFolders
    {
        [XmlElement(ElementName = "string")]
        public string String { get; set; }
    }


}