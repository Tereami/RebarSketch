using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RebarSketch
{
    public static class SupportSettings
    {
        public static string assemblyPath = "";
        public static string libraryPath = "";
        public static string tempPath;
        public static string fontName = "Isocpeur";
        public static float fontSize = 25;
        public static double lengthAccuracy = 5;
        public static System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;
        public static string imageParamName;

        public static bool Activate()
        {
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyFolder = System.IO.Path.GetDirectoryName(assemblyName);

            string programdataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string rbspath = Path.Combine(programdataPath, "RibbonBimStarter");
            if (!Directory.Exists(rbspath)) Directory.CreateDirectory(rbspath);
            string configPath = Path.Combine(rbspath, "config.ini");

            string weandrevitPath = "";
            if (File.Exists(configPath))
            {
                weandrevitPath = File.ReadAllLines(configPath)[0];
            }
            else
            {
                FormSelectPath form = new FormSelectPath();
                if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK) return false;
                if (form.CheckServerPath)
                {
                    weandrevitPath = form.ServerPath;
                }
                else
                {
                    weandrevitPath = rbspath;
                }
                File.WriteAllText(configPath, weandrevitPath);
            }

            //string weandrevitPath = FileSupport.GetMainWeandrevitFolder(addinPath);
            libraryPath = Path.Combine(weandrevitPath, "RebarSketch", "library");
            if (!Directory.Exists(libraryPath)) throw new Exception("Selected directory is not RebarSketch library path");

            string settingsFile = Path.Combine(weandrevitPath, "RebarSketch", "settings.txt");
            string[] settings = FileSupport.ReadFileWithAnyDecoding(settingsFile);
            fontName = settings[0].Split('#').Last();
            fontSize = float.Parse(settings[1].Split('#').Last());
            string textStyle = settings[2].Split('#').Last();
            fontStyle = FileSupport.GetFontStyle(textStyle);
            lengthAccuracy = double.Parse(settings[3].Split('#').Last());
            tempPath = settings[4].Split('#').Last();
            imageParamName = settings[5].Split('#').Last();


            FileSupport.CheckAndDeleteFolder(tempPath);
            return true;
        }
    }
}
