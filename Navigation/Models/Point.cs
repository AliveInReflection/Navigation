using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Navigation.Models
{
    [Serializable]
    public class Point
    {
        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public double Lat { get; set; }

        [XmlAttribute]
        public double Lng { get; set; }

        [XmlAttribute]
        public bool IsAirport { get; set; }

        [XmlAttribute]
        public string Name { get; set; }
    }
}
