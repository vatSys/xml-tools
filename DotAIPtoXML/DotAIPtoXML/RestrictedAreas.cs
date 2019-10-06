using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace DotAIPtoXML
{
    public class RestrictedAreas
    {
        public enum Patterns
        {
            Solid,
            Broken,
            Hash,
            Dashed,
            Dotted,
            None
        }

        public enum AreaTypes
        {
            Danger,
            Restricted
        }

        [XmlArrayItem("RestrictedArea")]
        public List<RestrictedArea> Areas = new List<RestrictedArea>();

        public class RestrictedArea
        {
            [XmlAttribute]
            public AreaTypes Type;
            [XmlAttribute]
            public string Name;
            [XmlAttribute]
            public int AltitudeFloor;
            [XmlAttribute]
            public int AltitudeCeiling;
            [XmlAttribute]
            public bool DAIWEnabled = false;
            [XmlAttribute]
            public Patterns LinePattern = Patterns.Solid;

            public Boundary Area = new Boundary();

            [XmlArray("Activations")]
            [XmlArrayItem("Activation")]
            public List<Activation> Activations = new List<Activation>();

            public RestrictedArea()
            {
            }
            public RestrictedArea(string name, AreaTypes type, int floor, int ceiling)
            {
                Name = name;
                Type = type;
                AltitudeFloor = floor;
                AltitudeCeiling = ceiling;
            }

            public class Activation
            {
                [XmlAttribute]
                public bool H24 = false;
                [XmlAttribute("Start")]
                public string RawStart
                {
                    get { return Start.ToString("HHmm"); }
                    set { Start = DateTime.ParseExact(value, "HHmm", System.Globalization.CultureInfo.InvariantCulture); }
                }
                [XmlIgnore]
                public DateTime Start;
                [XmlAttribute("End")]
                public string RawEnd
                {
                    get { return End.ToString("HHmm"); }
                    set { End = DateTime.ParseExact(value, "HHmm", System.Globalization.CultureInfo.InvariantCulture); }
                }
                [XmlIgnore]
                public DateTime End;

                public Activation() { }
                public Activation(bool h24)
                {
                    H24 = h24;
                }
                public Activation(string start, string end)
                {
                    RawStart = start;
                    RawEnd = end;
                }
            }

            public class Boundary
            {
                [XmlText]
                public string RawString
                {
                    get
                    {
                        string val = "";
                        if(List.Count>0)
                        {
                            val += List.First().ToString("ISO");
                            foreach (Coordinate c in List.Skip(1))
                                val += "/" + Environment.NewLine + c.ToString("ISO");
                        }
                        return val;
                    }
                    set
                    {
                        List.Clear();
                        string[] vals = value.Split('/');
                        foreach(string val in vals)
                        {
                            List.Add(new Coordinate(val));
                        }
                    }
                }

                [XmlIgnore]
                public List<Coordinate> List = new List<Coordinate>();
            }
        }
    }
}
