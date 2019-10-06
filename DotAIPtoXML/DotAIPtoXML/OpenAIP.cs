using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DotAIPtoXML
{
    [XmlRoot("OPENAIP")]
    public class OpenAIP
    {
        [XmlAttribute]
        public string VERSION;
        [XmlAttribute]
        public string DATAFORMAT;

        [XmlArray("AIRSPACES")]
        [XmlArrayItem("ASP")]
        public List<Airspace> Airspaces;

        public class Airspace
        {
            [XmlAttribute]
            public string CATEGORY;

            public string VERSION;
            public int ID;
            public string COUNTRY;
            public string NAME;
            public AltLimit ALTLIMIT_TOP;
            public AltLimit ALTLIMIT_BOTTOM;
            public Geometry GEOMETRY;
        }

        public class AltLimit
        {
            [XmlAttribute]
            public string REFERENCE;

            public Alt ALT;
        }

        public class Alt
        {
            [XmlAttribute]
            public string UNIT;
            [XmlText]
            public int Level;
        }

        public class Geometry
        {
            public string POLYGON;
        }
    }
}
