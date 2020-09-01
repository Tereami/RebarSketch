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

        public static string Activate()
        {
            string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string assemblyFolder = System.IO.Path.GetDirectoryName(assemblyName);

            string appdataFolder =
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bimstarterFolder =
                System.IO.Path.Combine(appdataFolder, "bim-starter");
            if (!System.IO.Directory.Exists(bimstarterFolder))
            {
                System.IO.Directory.CreateDirectory(bimstarterFolder);
            }
            string configPath = Path.Combine(bimstarterFolder, "config.ini");

            string weandrevitPath = "";
            if (File.Exists(configPath))
            {
                weandrevitPath = File.ReadAllLines(configPath)[0];
            }
            else
            {
                FormSelectPath form = new FormSelectPath(appdataFolder);
                if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK) return "Cancelled";
                if (form.UseServerPath)
                {
                    weandrevitPath = form.ServerPath;
                }
                else
                {
                    weandrevitPath = Path.Combine(appdataFolder, @"Autodesk\Revit\Addins\BimStarterConfig\");
                }
                File.WriteAllText(configPath, weandrevitPath);
            }

            //string weandrevitPath = FileSupport.GetMainWeandrevitFolder(addinPath);
            libraryPath = Path.Combine(weandrevitPath, "RebarSketch", "library");
            if (!Directory.Exists(libraryPath)) 
                return ("Library directory not exists: " + libraryPath);

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
            return string.Empty;
        }
    }
}
