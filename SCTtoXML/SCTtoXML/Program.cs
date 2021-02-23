using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace SCTtoXML
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("vatSys SCT to XML converter version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("Welcome...");
            Console.Write("Enter SCT File Name To Begin: ");

            string fname = Console.ReadLine();
            if (!fname.ToUpper().Contains(".SCT"))
                fname += ".sct";

            List<string> file = File.ReadLines(fname).ToList();
            Console.WriteLine(file.Count.ToString());

            Console.WriteLine("Do Airspace? (y/n)");
            if (Console.ReadLine() == "y")
                DoAirspace(file);

            Console.WriteLine("Do Sector from ARTCC? (y/n)");
            if (Console.ReadLine() == "y")
                DoSector(fname, file);

            Console.WriteLine("Do Geo Map? (y/n)");
            if (Console.ReadLine() == "y")
                DoGeo(fname, file);

            Console.WriteLine("Do Airspace Maps? (y/n)");
            if (Console.ReadLine() == "y")
                DoAirspaceMaps(fname, file);

            Console.WriteLine("Finished. Press any key to exit");
            Console.ReadKey();
        }

        static void DoSector(string fname, List<string> file)
        {
            fname = fname.Substring(0, fname.IndexOf('.'));
            List<string> info = GetSection(file, "[INFO]").Where(s => s.Length > 0 && s[0] != ';').ToList();
            string callsign = info[1];

            List<string> artcc = GetSection(file, "[ARTCC]");

            List<PointPair> unordered = new List<PointPair>();

            foreach (var line in artcc)
            {
                string[] ss = line.Split(' ');
                int start = 0;
                if (ss.Length > 4)
                    start++;

                unordered.Add(new PointPair(ConvertSectorLatLonToISO(ss[start]) + ConvertSectorLatLonToISO(ss[start + 1]), ConvertSectorLatLonToISO(ss[start + 2]) + ConvertSectorLatLonToISO(ss[start + 3])));
            }

            List<PointPair> ordered = new List<PointPair>();
            List<PointPair> cantFit = new List<PointPair>();

            ordered.Add(unordered[0]);//start with first item

            for (int i = 1; i < unordered.Count; i++)
            {
                if (InsertPoint(unordered[i], ordered, cantFit))
                {
                    bool testCants = true;
                    while (testCants)
                    {
                        testCants = false;
                        for (int j = 0; j < cantFit.Count; j++)
                        {
                            if (InsertPoint(cantFit[j], ordered, cantFit))
                            {
                                j--;
                                testCants = true;
                            }
                        }
                    }
                }
            }

            ordered.Reverse();

            XmlDocument volDoc = new XmlDocument();
            var vols = volDoc.AppendChild(volDoc.CreateElement("Volumes"));
            var bdry = vols.AppendChild(volDoc.CreateElement("Boundary"));
            ((XmlElement)bdry).SetAttribute("Name", fname);
            bdry.InnerText = string.Join("/"+Environment.NewLine, ordered.Select(s => s.p1));
            var vol = (XmlElement)vols.AppendChild(volDoc.CreateElement("Volume"));
            vol.SetAttribute("Name", fname);
            vol.SetAttribute("LowerLimit", "0");
            vol.SetAttribute("UpperLimit", "99999");
            var volBdrys = vol.AppendChild(volDoc.CreateElement("Boundaries"));
            volBdrys.InnerText = fname;

            XmlTextWriter writer = new XmlTextWriter("Volumes.xml", null);
            writer.Formatting = Formatting.Indented;
            volDoc.Save(writer);

            XmlDocument secDoc = new XmlDocument();
            var sectors = secDoc.AppendChild(secDoc.CreateElement("Sectors"));
            var sector = (XmlElement)sectors.AppendChild(secDoc.CreateElement("Sector"));
            sector.SetAttribute("Name", fname);
            sector.SetAttribute("Callsign", callsign);
            var secVols = sector.AppendChild(secDoc.CreateElement("Volumes"));
            secVols.InnerText = fname;

            writer = new XmlTextWriter("Sectors.xml", null);
            writer.Formatting = Formatting.Indented;
            secDoc.Save(writer);
        }

        static void DoAirspaceMaps(string fname, List<string> file)
        {
            fname = fname.Substring(0, fname.IndexOf('.'));
            List<string> info = GetSection(file, "[INFO]").Where(s=>s.Length > 0 && s[0] != ';').ToList();
            string centre = ConvertSectorLatLonToISO(info[3]) + ConvertSectorLatLonToISO(info[4]);

            XmlDocument doc = new XmlDocument();
            var maps = doc.AppendChild(doc.CreateElement("Maps"));

            var symbolNode = doc.CreateElement("Map");
            symbolNode.SetAttribute("Type", "System");
            symbolNode.SetAttribute("Name", $"{fname}_AIRSPACE");
            symbolNode.SetAttribute("Center", centre);
            symbolNode.SetAttribute("Priority", "2");

            var labelNode = doc.CreateElement("Map");
            labelNode.SetAttribute("Type", "System");
            labelNode.SetAttribute("Name", $"{fname}_NAMES");
            labelNode.SetAttribute("Center", centre);
            labelNode.SetAttribute("Priority", "1");

            DoAirportMaps(doc, symbolNode, labelNode, file);
            DoIntsMaps(doc, symbolNode, labelNode, file);
            DoARTCCMaps(doc, symbolNode, file);

            maps.AppendChild(symbolNode);
            maps.AppendChild(labelNode);

            XmlTextWriter writer = new XmlTextWriter($"{fname}_AIRSPACE.xml", null);
            writer.Formatting = Formatting.Indented;
            doc.Save(writer);
        }

        static void DoAirportMaps(XmlDocument doc, XmlNode symbolMap, XmlNode labelMap, List<string> file)
        {
            List<string> airportS = GetSection(file, "[AIRPORT]");

            var symbolNode = (XmlElement)symbolMap?.AppendChild(doc.CreateElement("Symbol"));
            symbolNode?.SetAttribute("Type", "Reticle");

            var labelNode = (XmlElement)labelMap?.AppendChild(doc.CreateElement("Label"));
            labelNode?.SetAttribute("HasLeader", "true");

            foreach (string line in airportS)
            {
                string portline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (portline.Length <= 0)
                    continue;

                string[] port = portline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //AYDU 000.000 S009.05.11.760 E143.12.29.459  C ; DARU
                if (port.Length != 5)
                    continue;

                var p = doc.CreateElement("Point");
                p.InnerText = port[0];
                var p2 = doc.CreateElement("Point");
                p2.InnerText = port[0];
                symbolNode?.AppendChild(p);
                labelNode?.AppendChild(p2);
            }
        }

        static void DoIntsMaps(XmlDocument doc, XmlNode symbols, XmlNode labels, List<string> file)
        {
            List<string> ints = GetSection(file, "[FIXES]");

            var symbolNode = (XmlElement)symbols?.AppendChild(doc.CreateElement("Symbol"));
            symbolNode?.SetAttribute("Type", "HollowTriangle");

            var labelNode = (XmlElement)labels?.AppendChild(doc.CreateElement("Label"));
            labelNode?.SetAttribute("HasLeader", "true");

            foreach (string line in ints)
            {
                string fixline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (fixline.Length <= 0)
                    continue;

                string[] fix = fixline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fix.Length != 3)
                    continue;

                var p = doc.CreateElement("Point");
                p.InnerText = fix[0];
                var p2 = doc.CreateElement("Point");
                p2.InnerText = fix[0];
                symbolNode?.AppendChild(p);
                labelNode?.AppendChild(p2);
            }

            symbolNode = (XmlElement)symbols?.AppendChild(doc.CreateElement("Symbol"));
            symbolNode?.SetAttribute("Type", "DotFillCircle");

            ints = GetSection(file, "[NDB]");
            foreach (string line in ints)
            {
                string fixline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (fixline.Length <= 0)
                    continue;

                string[] fix = fixline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fix.Length != 4)
                    continue;

                var p = doc.CreateElement("Point");
                p.InnerText = fix[0] + " NDB";
                var p2 = doc.CreateElement("Point");
                p2.InnerText = fix[0] + " NDB";
                symbolNode?.AppendChild(p);
                labelNode?.AppendChild(p2);
            }

            symbolNode = (XmlElement)symbols?.AppendChild(doc.CreateElement("Symbol"));
            symbolNode?.SetAttribute("Type", "Hexagon");

            ints = GetSection(file, "[VOR]");
            foreach (string line in ints)
            {
                string fixline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (fixline.Length <= 0)
                    continue;

                string[] fix = fixline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fix.Length != 4)
                    continue;

                var p = doc.CreateElement("Point");
                p.InnerText = fix[0] + " VOR";
                var p2 = doc.CreateElement("Point");
                p2.InnerText = fix[0] + " VOR";
                symbolNode?.AppendChild(p);
                labelNode?.AppendChild(p2);
            }
        }

        static void DoARTCCMaps(XmlDocument doc, XmlNode map, List<string> file)
        {
            List<string> artcc = GetSection(file, "[ARTCC]");
            foreach(var line in artcc)
            {
                string[] ss = line.Split(' ');
                int start = 0;
                if (ss.Length > 4)
                    start++;

                var l = map.AppendChild(doc.CreateElement("Line"));
                l.InnerText = ConvertSectorLatLonToISO(ss[start]) + ConvertSectorLatLonToISO(ss[start + 1]) + "/" + ConvertSectorLatLonToISO(ss[start + 2]) + ConvertSectorLatLonToISO(ss[start + 3]);
            }

            List<string> high = GetSection(file, "[ARTCC HIGH]");
            foreach (var line in high)
            {
                string[] ss = line.Split(' ');
                int start = 0;
                if (ss.Length > 4)
                    start++;

                var l = map.AppendChild(doc.CreateElement("Line"));
                l.InnerText = ConvertSectorLatLonToISO(ss[start]) + ConvertSectorLatLonToISO(ss[start + 1]) + "/" + ConvertSectorLatLonToISO(ss[start + 2]) + ConvertSectorLatLonToISO(ss[start + 3]);
            }

            List<string> low = GetSection(file, "[ARTCC LOW]");
            foreach (var line in low)
            {
                string[] ss = line.Split(' ');
                int start = 0;
                if (ss.Length > 4)
                    start++;

                var l = map.AppendChild(doc.CreateElement("Line"));
                ((XmlElement)l).SetAttribute("Pattern", "Dotted");
                l.InnerText = ConvertSectorLatLonToISO(ss[start]) + ConvertSectorLatLonToISO(ss[start + 1]) + "/" + ConvertSectorLatLonToISO(ss[start + 2]) + ConvertSectorLatLonToISO(ss[start + 3]);
            }
        }

        static void DoAirspace(List<string> file)
        {
            XmlDocument doc = new XmlDocument();
            var airspace = doc.AppendChild(doc.CreateElement("Airspace"));
            DoAirports(doc, airspace, file);
            //DoAirways(doc, airspace, file);
            DoInts(doc, airspace, file);
            XmlTextWriter writer = new XmlTextWriter("Airspace.xml", null);
            writer.Formatting = Formatting.Indented;
            doc.Save(writer);
        }

        static void DoAirports(XmlDocument doc, XmlNode parent, List<string> file)
        {
            List<string> airportS = GetSection(file, "[AIRPORT]");

            XmlElement airports = (XmlElement)parent.AppendChild(doc.CreateElement("Airports"));

            foreach (string line in airportS)
            {
                string portline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (portline.Length <= 0)
                    continue;

                string[] port = portline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //AYDU 000.000 S009.05.11.760 E143.12.29.459  C ; DARU
                if (port.Length != 5)
                    continue;

                XmlElement x = (XmlElement)airports.AppendChild(doc.CreateElement("Airport"));
                x.SetAttribute("ICAO", port[0]);
                string iso = ConvertSectorLatLonToISO(port[2]);
                iso += ConvertSectorLatLonToISO(port[3]);
                x.SetAttribute("Position", iso);
            }

            Console.WriteLine(airportS.Count.ToString() + " Airports Created.");
        }

        static List<string> GetSection(List<string> file, string section)
        {
            return file.SkipWhile(s => !s.Contains(section)).Where(s => s.Trim() != "" && s.Trim() != section).TakeWhile(s => !s.Contains("[")).ToList();
        }

        static void DoAirways(XmlDocument doc, XmlNode parent, List<string> file)
        {
            XmlElement xmlAirways = (XmlElement)parent.AppendChild(doc.CreateElement("Airways"));

            string section = "[HIGH AIRWAY]";
            int count = 0;
            List<string> awys = GetSection(file, section);
            count += awys.Count;

            List<Airway> airways = GetOrderedAirways(awys);
            section = "[LOW AIRWAY]";
            awys = GetSection(file, section);
            count += awys.Count;
            airways.AddRange(GetOrderedAirways(awys));

            List<Airway> dupe = airways.GroupBy(a => a.Name).Where(a=>a.Count()>1).SelectMany(grp => grp.OrderByDescending(a=>a.OrderedPoints.Count)).ToList();
            foreach(Airway d in dupe)
            {
                if (!airways.Contains(d))
                    continue;

                airways.RemoveAll(a => a.Name == d.Name && a != d);
            }
            dupe = airways.GroupBy(a => a.Name).Where(a => a.Count() > 1).SelectMany(grp => grp.OrderByDescending(a => a.OrderedPoints.Count)).ToList();

            foreach (Airway a in airways)
            {
                XmlElement x = (XmlElement)xmlAirways.AppendChild(doc.CreateElement("Airway"));
                x.SetAttribute("Name", a.Name);
                x.InnerText = Environment.NewLine;
                foreach (PointPair points in a.OrderedPoints)
                {
                    x.InnerText += points.p1 + "/" + Environment.NewLine;
                    if (points == a.OrderedPoints.Last())
                        x.InnerText += points.p2 + Environment.NewLine;
                }
            }

            Console.WriteLine(count.ToString() + " airway lines processed.");
            
            if (dupe.Count > 0)
            {
                Console.WriteLine("WARNING: " + dupe.ToString() + " unresolved duplicate airways!");
                Console.WriteLine("List? (y/n)");
                if(Console.ReadLine()=="y")
                {
                    foreach (Airway a in dupe)
                        Console.WriteLine(a.Name);
                }
            }
        }

        static List<Airway> GetOrderedAirways(List<string> awys)
        {
            List<Airway> airways = new List<Airway>();

            foreach (string line in awys)
            {
                string awyline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (awyline.Length <= 0)
                    continue;

                string[] awy = awyline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (awy.Length != 5)
                    continue;

                string name = new string(awy[0].TakeWhile(c => c != '(').ToArray());//strip (xx) shit vatpac adds.

                Airway airway = airways.SingleOrDefault(a => a.Name == name);
                if (airway == null)
                {
                    airway = new Airway(name);
                    airways.Add(airway);
                }

                string point1 = "";
                if (awy[1] == awy[2])//then not a lat lon
                    point1 = awy[1];
                else
                    point1 = ConvertSectorLatLonToISO(awy[1]) + ConvertSectorLatLonToISO(awy[2]);

                string point2 = "";
                if (awy[3] == awy[4])//then not a lat lon
                    point2 = awy[3];
                else
                    point2 = ConvertSectorLatLonToISO(awy[3]) + ConvertSectorLatLonToISO(awy[4]);

                airway.UnorderedPoints.Add(new PointPair(point1, point2));
            }

            foreach (Airway a in airways)
            {
                if (a.UnorderedPoints.Count < 1)
                    continue;

                List<PointPair> ordered = new List<PointPair>();
                List<PointPair> cantFit = new List<PointPair>();
                ordered.Add(a.UnorderedPoints[0]);//start with first item

                for (int i = 1; i < a.UnorderedPoints.Count; i++)
                {
                    if (InsertPoint(a.UnorderedPoints[i], ordered, cantFit))
                    {
                        bool testCants = true;
                        while (testCants)
                        {
                            testCants = false;
                            for (int j = 0; j < cantFit.Count; j++)
                            {
                                if (InsertPoint(cantFit[j], ordered, cantFit))
                                {
                                    j--;
                                    testCants = true;
                                }
                            }
                        }
                    }
                }

                ordered.Reverse();
                a.OrderedPoints = ordered;
            }

            return airways;
        }

        static bool InsertPoint(PointPair testPoint, List<PointPair> ordered, List<PointPair> cantFit)
        {
            int p1, p2;
            p1 = ordered.FindIndex(p => p.p1 == testPoint.p2);//goes after
            p2 = ordered.FindIndex(p => p.p2 == testPoint.p1);//goes before
            if (p1 < 0 && p2 < 0)
            {
                if (!cantFit.Contains(testPoint))
                    cantFit.Add(testPoint);
                return false;
            }
            else
            {
                if (p2 < 0)
                    ordered.Insert(p1 + 1, testPoint);//after
                else
                    ordered.Insert(p2, testPoint);//before

                if (cantFit.Contains(testPoint))
                    cantFit.Remove(testPoint);

                return true;
            }
        }

        public class PointPair
        {
            public string p1, p2;
            public PointPair(string point1, string point2)
            {
                p1 = point1;
                p2 = point2;
            }
        }

        public class Airway
        {
            public string Name;
            public List<PointPair> UnorderedPoints = new List<PointPair>();
            public List<PointPair> OrderedPoints = new List<PointPair>();

            public Airway(string name)
            {
                Name = name;
            }
        }

        static void DoInts(XmlDocument doc, XmlNode parent, List<string> file)
        {
            int count = 0;
            List<string> ints = GetSection(file, "[FIXES]");
            count += ints.Count;

            XmlElement intersections = (XmlElement)parent.AppendChild(doc.CreateElement("Intersections"));

            foreach (string line in ints)
            {
                string fixline = new string(line.Trim().TakeWhile(c=>c!=';').ToArray());
                if (fixline.Length <= 0)
                    continue;

                string[] fix = fixline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fix.Length != 3)
                    continue;

                XmlElement x = (XmlElement)intersections.AppendChild(doc.CreateElement("Point"));
                x.SetAttribute("Name", fix[0]);
                x.SetAttribute("Type", "Fix");
                x.InnerText = ConvertSectorLatLonToISO(fix[1]);
                x.InnerText += ConvertSectorLatLonToISO(fix[2]);
            }

            ints = GetSection(file, "[NDB]");
            foreach (string line in ints)
            {
                string fixline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (fixline.Length <= 0)
                    continue;

                string[] fix = fixline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fix.Length != 4)
                    continue;

                XmlElement x = (XmlElement)intersections.AppendChild(doc.CreateElement("Point"));
                x.SetAttribute("Name", fix[0]);
                x.SetAttribute("Type", "Navaid");
                x.SetAttribute("NavaidType", "NDB");
                x.SetAttribute("Frequency", fix[1]);
                x.InnerText = ConvertSectorLatLonToISO(fix[2]);
                x.InnerText += ConvertSectorLatLonToISO(fix[3]);
            }
            count += ints.Count;

            ints = GetSection(file, "[VOR]");
            foreach (string line in ints)
            {
                string fixline = new string(line.Trim().TakeWhile(c => c != ';').ToArray());
                if (fixline.Length <= 0)
                    continue;

                string[] fix = fixline.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fix.Length != 4)
                    continue;

                XmlElement x = (XmlElement)intersections.AppendChild(doc.CreateElement("Point"));
                x.SetAttribute("Name", fix[0]);
                x.SetAttribute("Type", "Navaid");
                x.SetAttribute("NavaidType", "VOR");
                x.SetAttribute("Frequency", fix[1]);
                x.InnerText = ConvertSectorLatLonToISO(fix[2]);
                x.InnerText += ConvertSectorLatLonToISO(fix[3]);
            }
            count += ints.Count;
            Console.WriteLine(count.ToString() + " Intersections Created.");
        }

        static void DoGeo(string fname, List<string> file)
        {
            List<string> geo = GetSection(file, "[GEO]");
            Console.WriteLine("[GEO] Lines: " + geo.Count.ToString());

            XmlDocument doc = new XmlDocument();
            XmlElement maps = (XmlElement)doc.AppendChild(doc.CreateElement("Maps"));
            XmlElement map = (XmlElement)maps.AppendChild(doc.CreateElement("Map"));

            fname = fname.Substring(0, fname.IndexOf('.'));
            List<string> info = GetSection(file, "[INFO]").Where(s => s.Length > 0 && s[0] != ';').ToList();
            string centre = ConvertSectorLatLonToISO(info[3]) + ConvertSectorLatLonToISO(info[4]);

            map.SetAttribute("Type", "System");
            map.SetAttribute("Name", $"{fname}_GEO");
            map.SetAttribute("Center", centre);
            map.SetAttribute("Priority", "2");

            Console.WriteLine("Filter by colour? (Enter Colour Name): ");
            string colour = Console.ReadLine();
            List<string> slines = geo;
            if (!string.IsNullOrEmpty(colour))
                slines = geo.FindAll(s => s.Contains(colour)).ToList();

            for (int i = 0; i < slines.Count; i++)
            {
                string[] ss = slines[i].Trim().Split(' ');
                if (ss.Length < 5)
                    continue;

                int start = 0;
                if (ss.Length > 5)
                    start++;

                XmlElement line = (XmlElement)map.AppendChild(doc.CreateElement("Line"));
                line.InnerText += ConvertSectorLatLonToISO(ss[start]);
                line.InnerText += ConvertSectorLatLonToISO(ss[start+1]);
                line.InnerText += "/";
                line.InnerText += ConvertSectorLatLonToISO(ss[start+2]);
                line.InnerText += ConvertSectorLatLonToISO(ss[start+3]);
            }

            XmlTextWriter writer = new XmlTextWriter(fname + "_GEO" + ".xml", null);
            writer.Formatting = Formatting.Indented;
            doc.Save(writer);
        }

        static string ConvertSectorLatLonToISO(string splitstring)
        {
            string line = "";

            bool isLon = splitstring.Contains('E') || splitstring.Contains('W');

            if (splitstring.Contains('S') || splitstring.Contains('W'))
                line += "-";
            else
                line += "+";

            string l = splitstring.Replace("S", "").Replace("N", "").Replace("E", "").Replace("W", "");
            l = l.TrimStart(new char[] { '0' });

            string[] sl = l.Split('.');
            if (sl.Length != 4)
                return "";

            if(isLon)
                line += sl[0].PadLeft(3, '0') + sl[1].PadLeft(2, '0') + sl[2].PadLeft(2, '0') + "." + sl[3];
            else
                line += sl[0].PadLeft(2,'0') + sl[1].PadLeft(2, '0') + sl[2].PadLeft(2, '0') + "." + sl[3];
            return line;
        }
    }
}
