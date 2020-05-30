using System;
using System.Collections.Generic;
using System.Linq;
using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.IO;
using System.Xml;

namespace KMLtoMap
{
    class Program
    {
        static KmlFile kmlFile = null;
        static float SMALL_RADIUS = 0.2f;

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            string spacer = "----------------------------------------------------------------------------------------------------";
            Console.WriteLine(spacer);
            Console.WriteLine("KMLtoMap version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine(spacer);

            LoadFile();

            if (kmlFile == null)
                return;

            Document doc = (Document)kmlFile.Root.Flatten().OfType<Document>().First();

            string name = doc.Name.Replace(".kml", "");

            Console.WriteLine("Map Type: (eg. System, System2");
            string type = Console.ReadLine();
            Console.WriteLine("Priority:");
            string priority = Console.ReadLine();
            Console.WriteLine("Center:");
            string center = Console.ReadLine();
            Console.WriteLine("Line or Infill? (l/i)? ");
            bool infill = Console.ReadKey().KeyChar == 'i';
            string pattern = "";
            if (!infill)
            {
                Console.WriteLine("Line Pattern:");
                pattern = Console.ReadLine();
            }
            Console.WriteLine("Weld Nearby Coordinates (y/n)?");
            bool weld = Console.ReadKey().KeyChar == 'y';
            Console.WriteLine("Add Placemark name as a label at polygon center (y/n)? ");
            bool label = Console.ReadKey().KeyChar == 'y';
            Console.WriteLine();
            Console.WriteLine("Creating " + name + "...");

            XmlWriterSettings xSettings = new XmlWriterSettings();
            xSettings.Indent = true;

            XmlWriter xml = XmlWriter.Create(name + ".xml", xSettings);
            xml.WriteStartDocument();
            xml.WriteStartElement("Maps");
            xml.WriteStartElement("Map");
            xml.WriteAttributeString("Type", type);
            xml.WriteAttributeString("Name", name);
            xml.WriteAttributeString("Priority", priority);
            xml.WriteAttributeString("Center", center);

            List<Coordinate> previousCoordinates = new List<Coordinate>();
            foreach (Placemark mark in doc.Features.OfType<Folder>().First().Features.OfType<Placemark>())
            {
                foreach(Point point in mark.Flatten().OfType<Point>())
                {
                    xml.WriteStartElement("Label");
                    xml.WriteAttributeString("HasLeader", "false");
                    xml.WriteStartElement("Point");
                    xml.WriteAttributeString("Name", mark.Name);
                    Coordinate c = new Coordinate(point.Coordinate);
                    xml.WriteString(c.ToString("ISO"));
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                }

                foreach(LineString line in mark.Flatten().OfType<LineString>())
                {
                    if (label)
                    {
                        xml.WriteStartElement("Label");
                        xml.WriteAttributeString("HasLeader", "false");
                        xml.WriteStartElement("Point");
                        xml.WriteAttributeString("Name", mark.Name);
                        Coordinate c = new Coordinate(line.CalculateBounds().Center);
                        xml.WriteString(c.ToString("ISO"));
                        xml.WriteEndElement();
                        xml.WriteEndElement();
                    }

                    if(infill)
                        xml.WriteStartElement("Infill");
                    else
                        xml.WriteStartElement("Line");
                    if(pattern != "")
                        xml.WriteAttributeString("Pattern", pattern);
                    xml.WriteAttributeString("Name", mark.Name);
                    xml.WriteStartElement("Point");
                    for (int i = 0; i < line.Coordinates.Count; i++)
                    {
                        Coordinate c = new Coordinate(line.Coordinates.ElementAt(i));
                        Coordinate collapse = previousCoordinates.FirstOrDefault(pc => Conversions.CalculateDistance(pc, c) < SMALL_RADIUS);
                        if (collapse != null && weld)
                            c = collapse;
                        else
                            previousCoordinates.Add(c);

                        if (i == line.Coordinates.Count - 1)
                            xml.WriteString(c.ToString("ISO"));
                        else
                            xml.WriteString(c.ToString("ISO/"));
                    }
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                }
                foreach(Polygon poly in mark.Flatten().OfType<Polygon>())
                {
                    if(label)
                    {
                        xml.WriteStartElement("Label");
                        xml.WriteAttributeString("HasLeader", "false");
                        xml.WriteStartElement("Point");
                        xml.WriteAttributeString("Name", mark.Name);
                        Coordinate c = new Coordinate(poly.CalculateBounds().Center);
                        xml.WriteString(c.ToString("ISO"));
                        xml.WriteEndElement();
                        xml.WriteEndElement();
                    }

                    if (infill)
                        xml.WriteStartElement("Infill");
                    else
                        xml.WriteStartElement("Line");
                    if (pattern != "")
                        xml.WriteAttributeString("Pattern", pattern);
                    xml.WriteAttributeString("Name", mark.Name);
                    xml.WriteStartElement("Point");
                    for (int i = 0; i< poly.OuterBoundary.LinearRing.Coordinates.Count; i++)
                    {
                        Coordinate c = new Coordinate(poly.OuterBoundary.LinearRing.Coordinates.ElementAt(i));
                        Coordinate collapse = previousCoordinates.FirstOrDefault(pc => Conversions.CalculateDistance(pc, c) < SMALL_RADIUS);
                        if (collapse != null && weld)
                            c = collapse;
                        else
                            previousCoordinates.Add(c);

                        if (i == poly.OuterBoundary.LinearRing.Coordinates.Count - 1)
                            xml.WriteString(c.ToString("ISO"));
                        else
                            xml.WriteString(c.ToString("ISO/"));
                    }
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndDocument();
            xml.Close();

            Console.WriteLine("Done!");
            Console.Read();
        }

        static void LoadFile()
        {
            FileStream fs;
            KmlFile file = null;
            Console.WriteLine("Enter KML filepath:");
            try
            {
                fs = File.OpenRead(Console.ReadLine());
                file = KmlFile.Load(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write("Retry/Exit (r/x)? ");
                if (Console.ReadKey().KeyChar=='r')
                {
                    Console.WriteLine();
                    LoadFile();
                    return;
                }
                else
                    Environment.Exit(0);
            }

            kmlFile = file;
        }
    }
}
