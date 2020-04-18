using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace CoastlineGENConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome...");
            Console.Write("Enter File Name To Begin: ");

            string fname = Console.ReadLine();

            List<string> file = File.ReadLines(fname).ToList();
            Console.WriteLine(file.Count.ToString());
            
            XmlDocument doc = new XmlDocument();

            XmlElement maps = (XmlElement)doc.AppendChild(doc.CreateElement("Maps"));
            XmlElement map = (XmlElement)maps.AppendChild(doc.CreateElement("Map"));

            XmlElement line = null;
            string textdata = "";
            bool first = true;
            for (int i = 0; i < file.Count; i++)
            {
                if (file[i].Trim() == "END" && !first || line == null)
                {
                    if(line!=null && textdata!="")
                    {
                        line.AppendChild(doc.CreateTextNode(textdata));
                    }
                    if (i + 1 < file.Count && !file[i + 1].Contains("END"))
                        line = (XmlElement)map.AppendChild(doc.CreateElement("Line"));
                    textdata = "";
                    first = true;
                }

                if (!file[i].Contains(','))
                    continue;

                string[] ss = file[i].Split(',');
                if (ss.Length != 2)
                    continue;

                line.SetAttribute("Name", "Coastline");
                textdata += Environment.NewLine + "  " + "  " + "  ";
                textdata += ConvertGENLatLonToISO(ss[1], false) + ConvertGENLatLonToISO(ss[0], true);
                if (i + 1 < file.Count && !file[i + 1].Contains("END"))
                    textdata += "/";

                first = false;
            }

            fname = fname.Substring(0, fname.IndexOf('.'));

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.None
            };

            using (XmlWriter writer = XmlWriter.Create(fname + ".xml", settings))
            {
                doc.Save(writer);
            }
            //doc.Save(fname + ".xml");
        }

        static string ConvertGENLatLonToISO(string splitstring, bool isLongitude)
        {
            string line = "";

            if (!splitstring.Contains('-'))
                line += "+";
            else
                line += "-";

            string val = splitstring.Trim().TrimStart(new char[]{'-'});

            int zeros = 0;
            if (isLongitude && val.IndexOf('.') != 3)
                zeros = 3 - val.IndexOf('.');
            else if (!isLongitude && val.IndexOf('.') != 2)
                zeros = 2 - val.IndexOf('.');

            for (int i = 0; i < zeros; i++)
                val = val.Insert(0, "0");

            line += val;
            return line;
        }
    }
}
