using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DotAIPtoXML
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            string spacer = "----------------------------------------------------------------------------------------------------";
            Console.WriteLine(spacer);
            Console.WriteLine(".AIPtoXML version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine(spacer);

            FileStream fs = LoadFile();

            XmlSerializer ser = new XmlSerializer(typeof(OpenAIP));
            OpenAIP data = (OpenAIP)ser.Deserialize(fs);
            Console.WriteLine("Read " + data.Airspaces.Count + " airspaces.");

            RestrictedAreas areas = new RestrictedAreas();

            foreach(OpenAIP.Airspace asp in data.Airspaces)
            {
                RestrictedAreas.AreaTypes type = RestrictedAreas.AreaTypes.Restricted;
                if (asp.CATEGORY == "DANGER")
                    type = RestrictedAreas.AreaTypes.Danger;
                else if (asp.CATEGORY != "RESTRICTED")
                    continue;

                int floor = asp.ALTLIMIT_BOTTOM.ALT.Level;
                if (asp.ALTLIMIT_BOTTOM.ALT.UNIT == "FL")
                    floor = floor * 100;
                int ceil = asp.ALTLIMIT_TOP.ALT.Level;
                if (asp.ALTLIMIT_TOP.ALT.UNIT == "FL")
                    ceil = ceil * 100;

                RestrictedAreas.RestrictedArea ra = new RestrictedAreas.RestrictedArea(asp.NAME, type, floor, ceil);
                if (type == RestrictedAreas.AreaTypes.Danger)
                    ra.LinePattern = RestrictedAreas.Patterns.None;
                else
                    ra.DAIWEnabled = true;

                string[] lls = asp.GEOMETRY.POLYGON.Split(',');
                foreach(string ll in lls)
                {
                    string[] llsplit = ll.Trim().Split(' ');
                    Coordinate c = new Coordinate(float.Parse(llsplit[1]), float.Parse(llsplit[0]));
                    ra.Area.List.Add(c);
                }

                if(asp.NAME.Contains("[H24]"))
                {
                    ra.Activations.Add(new RestrictedAreas.RestrictedArea.Activation(true));
                }

                ra.Name = ra.Name.Replace("[NOTAM]", "").Replace("[H24]","").Trim();

                if (ra.Area.List.Count > 2)
                    areas.Areas.Add(ra);
                else
                    Console.WriteLine(ra.Name + " was rejected.");

            }

            XmlSerializer raSer = new XmlSerializer(typeof(RestrictedAreas));
            TextWriter writer = new StreamWriter("RestrictedAreas.xml");
            raSer.Serialize(writer, areas);
            writer.Close();
            Console.WriteLine("Wrote " + areas.Areas.Count.ToString() + " areas to vatsysXML.");

            Console.ReadKey();
        }

        static FileStream LoadFile()
        {
            FileStream fs = null;

            Console.WriteLine("Enter .AIP filepath:");
            try
            {
                fs = File.OpenRead(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write("Retry/Exit (r/x)? ");
                if (Console.ReadKey().KeyChar == 'r')
                {
                    Console.WriteLine();
                    return LoadFile(); 
                }
                else
                    Environment.Exit(0);
            }

            return fs;
        }
    }
}
