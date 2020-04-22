using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace RebarSketch
{
    public static class FileSupport
    {
        public static string[] ReadFileWithAnyDecoding(string filePath)
        {
            bool check = File.Exists(filePath);
            if (!check)
            {
                throw new Exception("Не найден файл: " + filePath);
            }
            Encoding e = GetEncoding(filePath);
            string[] familiesNames = File.ReadAllLines(filePath, e);
            return familiesNames;
        }


        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
                file.Close();
            }

            // Analyze the BOM

            if (bom[0] == 0xd0 && bom[1] == 0xbe && bom[2] == 0xd0 && bom[3] == 0xb1) return Encoding.UTF8;
            if (bom[0] == 0x32 && bom[1] == 0x36 && bom[2] == 0x31 && bom[3] == 0x5f) return Encoding.GetEncoding(1251);
            return Encoding.UTF8;
        }


        public static void CheckAndDeleteFolder(string path)
        {
            bool checkExists = System.IO.Directory.Exists(path);
            if (!checkExists) return;

            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            System.IO.Directory.Delete(path);
        }


        public static System.Drawing.FontStyle GetFontStyle(string text)
        {
            if (text == "Bold") return System.Drawing.FontStyle.Bold;
            if (text == "Italic") return System.Drawing.FontStyle.Italic;
            return System.Drawing.FontStyle.Regular;
        }

        public static string GetMainWeandrevitFolder(string addinPath)
        {
            bool check = File.Exists(addinPath);
            string gatesPath = "";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(addinPath);
            XmlElement xRoot = xDoc.DocumentElement;
            XmlNode addinNode = xRoot.SelectSingleNode("AddIn");
            XmlNode pathNode = addinNode.SelectSingleNode("GatesPath");
            gatesPath = pathNode.InnerText;

            return gatesPath;
        }
    }
}
