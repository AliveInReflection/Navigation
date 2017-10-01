using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Navigation.Models
{
    [Serializable]
    public class StorageModel
    {
        //[XmlArray]
        public Point[] Points { get; set; }

        //[XmlArray]
        public Connection[] Connections { get; set; }
    }
}
