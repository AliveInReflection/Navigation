using System;
using System.Xml.Serialization;

namespace Navigation.Models
{
    [Serializable]
    public class Connection
    {
        [XmlAttribute]
        public int From { get; set; }

        [XmlAttribute]
        public int To { get; set; }

        [XmlAttribute]
        public bool TwoWay { get; set; }
    }
}
