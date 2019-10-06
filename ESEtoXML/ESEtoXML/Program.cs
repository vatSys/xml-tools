using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace ESEtoXML
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("vatSys EuroScope ESE to XML Converter");
            Console.Write("Enter File Name To Begin: ");

            string fname = Console.ReadLine();

            List<string> file = File.ReadLines(fname).ToList();
            Console.WriteLine(file.Count.ToString() + " lines read.");

            Console.WriteLine("Program will now generate XML from [POSITIONS] & [AIRSPACE]...");
            string search = "[POSITIONS]";

            List<string> slines = file.SkipWhile(s => !s.Contains(search)).Where(s => s.Trim() != "" && s.Trim() != search).TakeWhile(s => !s.Contains("[")).ToList();
            Console.WriteLine(slines.Count.ToString() + " lines found under " + search + ".");

            XmlDocument sectorsdoc = new XmlDocument();
            XmlDocument ssrDoc = new XmlDocument();
            XmlElement sectors = (XmlElement)sectorsdoc.AppendChild(sectorsdoc.CreateElement("Sectors"));
            XmlElement ssrass = (XmlElement)ssrDoc.AppendChild(ssrDoc.CreateElement("SSRAssignment"));

            for (int i = 0; i < slines.Count; i++)
            {
                List<string> ss = slines[i].Trim().Split(':').ToList();

                foreach (string s in ss.ToList())
                {
                    if (s.Length == 0)
                        ss.Remove(s);
                }

                if (ss.Count < 11)
                    continue;

                XmlElement sector = (XmlElement)sectors.AppendChild(sectorsdoc.CreateElement("Sector"));
                sector.SetAttribute("FullName", ss[0]);
                sector.SetAttribute("Frequency", ss[2]);
                sector.SetAttribute("Callsign", ss[5] + "_" + ss[6]);
                string name = ss[8];
                if (ss[8] == "OCE")
                    name = ss[7];
                sector.SetAttribute("Name", name);

                XmlElement bin = (XmlElement)ssrass.AppendChild(ssrDoc.CreateElement("Bin"));
                XmlElement codes = (XmlElement)bin.AppendChild(ssrDoc.CreateElement("Codes"));
                codes.SetAttribute("Start", ss[9].PadLeft(4, '0'));
                codes.SetAttribute("End", ss[10].PadLeft(4, '0'));
                XmlElement rule = (XmlElement)codes.AppendChild(ssrDoc.CreateElement("Rule"));
                rule.SetAttribute("Sector", name);
            }

            fname = fname.Substring(0, fname.IndexOf('.'));
            XmlTextWriter writer = new XmlTextWriter(fname + "_" + search + ".xml", null);
            writer.Formatting = Formatting.Indented;
            sectorsdoc.Save(writer);

            writer = new XmlTextWriter(fname + "_" + "SSR.xml", null);
            writer.Formatting = Formatting.Indented;
            ssrDoc.Save(writer);

            search = "[AIRSPACE]";

            slines = file.SkipWhile(s => !s.Contains(search)).Where(s => s.Trim() != "" && s.Trim() != search).TakeWhile(s => !s.Contains("[")).ToList();
            Console.WriteLine(slines.Count.ToString() + " lines found under " + search + ".");

            XmlDocument airDoc = new XmlDocument();
            XmlElement volumes = (XmlElement)airDoc.AppendChild(airDoc.CreateElement("Volumes"));

            bool sectorline = false;
            XmlElement boundary = null;
            bool sectordef = false;
            XmlElement volume = null;

            for (int i = 0; i < slines.Count; i++)
            {
                List<string> ss = slines[i].Trim().Split(':').ToList();

                foreach (string s in ss.ToList())
                {
                    if (s.Length == 0)
                        ss.Remove(s);
                }

                if (ss.Count < 2)
                    continue;

                if(ss[0]=="SECTORLINE")
                {
                    boundary = (XmlElement)volumes.AppendChild(airDoc.CreateElement("Boundary"));
                    boundary.SetAttribute("Name", ss[1]);
                    sectorline = true;
                    sectordef = false;
                }
                else if(ss[0]=="SECTOR")
                {
                    volume = (XmlElement)volumes.AppendChild(airDoc.CreateElement("Volume"));
                    volume.SetAttribute("Name", ss[1]);
                    volume.SetAttribute("LowerLimit", ss[2]);
                    volume.SetAttribute("UpperLimit", ss[3]);
                    sectordef = true;
                    sectorline = false;
                }

                if (sectorline && ss[0] == "COORD")
                {
                    if (boundary.InnerText == "")
                        boundary.InnerText = ConvertSectorLatLonToISO(ss[1]) + ConvertSectorLatLonToISO(ss[2]);
                    else
                        boundary.InnerText += "/" + Environment.NewLine + ConvertSectorLatLonToISO(ss[1]) + ConvertSectorLatLonToISO(ss[2]);
                }

                if(sectordef && ss[0] == "BORDER")
                {
                    XmlElement boundaries = (XmlElement)volume.AppendChild(airDoc.CreateElement("Boundaries"));
                    for(int j = 1; j<ss.Count; j++)
                    {
                        if (j == 1)
                            boundaries.InnerText = ss[j];
                        else
                            boundaries.InnerText += "," + ss[j];
                    }
                }
            }

            writer = new XmlTextWriter(fname + "_" + "VOLUMES.xml", null);
            writer.Formatting = Formatting.Indented;
            airDoc.Save(writer);

            search = "[SIDSSTARS]";

            slines = file.SkipWhile(s => !s.Contains(search)).Where(s => s.Trim() != "" && s.Trim() != search).TakeWhile(s => !s.Contains("[")).ToList();
            Console.WriteLine(slines.Count.ToString() + " lines found under " + search + ".");

            XmlDocument ssDoc = new XmlDocument();
            XmlElement sidstars = (XmlElement)airDoc.AppendChild(airDoc.CreateElement("SIDSTARs"));

            foreach(string line in slines)
            {
                string[] ss = line.Trim().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (ss.Length != 5)
                    continue;

                foreach(string ssPart in ss)
                {

                }
            }

            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static string ConvertSectorLatLonToISO(string splitstring)
        {
            string line = "";

            if (splitstring.Contains('S') || splitstring.Contains('W'))
                line += "-";
            else
                line += "+";

            bool isLongitude = splitstring.Contains('W') || splitstring.Contains('E');

            string l = splitstring.Replace("S", "").Replace("N", "").Replace("E", "").Replace("W", "");
            l = l.TrimStart(new char[] { '0' });

            string[] sl = l.Split('.');
            if (sl.Length != 4)
                return "";

            int zeros = 0;
            if (isLongitude && sl[0].Length != 3)
                zeros = 3 - sl[0].Length;
            else if (!isLongitude && sl[0].Length != 2)
                zeros = 2 - sl[0].Length;

            for (int i = 0; i < zeros; i++)
                sl[0] = sl[0].Insert(0, "0");

            line += sl[0] + sl[1] + sl[2] + "." + sl[3];
            return line;
        }
    }
}
