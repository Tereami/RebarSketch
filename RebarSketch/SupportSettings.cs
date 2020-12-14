#region License
/*Данный код опубликован под лицензией Creative Commons Attribution-ShareAlike.
Разрешено использовать, распространять, изменять и брать данный код за основу для производных в коммерческих и
некоммерческих целях, при условии указания авторства и если производные лицензируются на тех же условиях.
Код поставляется "как есть". Автор не несет ответственности за возможные последствия использования.
Зуев Александр, 2020, все права защищены.
This code is listed under the Creative Commons Attribution-ShareAlike license.
You may use, redistribute, remix, tweak, and build upon this work non-commercially and commercially,
as long as you credit the author by linking back and license your new creations under the same terms.
This code is provided 'as is'. Author disclaims any implied warranty.
Zuev Aleksandr, 2020, all rigths reserved.*/
#endregion
#region Usings
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
#endregion

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
                Debug.WriteLine("Create folder: " + bimstarterFolder);
                System.IO.Directory.CreateDirectory(bimstarterFolder);
            }
            string configPath = Path.Combine(bimstarterFolder, "config.ini");

            string weandrevitPath = "";
            if (File.Exists(configPath))
            {
                Debug.WriteLine("Read file: " + configPath);
                weandrevitPath = File.ReadAllLines(configPath)[0];
            }
            else
            {
                Debug.WriteLine("First start, show dialog window and select config folder");
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
                Debug.WriteLine("Selected user path: " + weandrevitPath);
                File.WriteAllText(configPath, weandrevitPath);
                Debug.WriteLine("Written to file: " + configPath);
            }

            libraryPath = Path.Combine(weandrevitPath, "RebarSketch", "library");
            Debug.WriteLine("Library path: " + libraryPath);
            if (!Directory.Exists(libraryPath)) 
                return ("Library directory not exists: " + libraryPath);

            string settingsFile = Path.Combine(weandrevitPath, "RebarSketch", "settings.txt");
            Debug.WriteLine("Settings path: " + settingsFile);
            if(!File.Exists(settingsFile))
            {
                Debug.WriteLine("File not found: " + settingsFile);
                throw new Exception("File not found: " + settingsFile);
            }
            string[] settings = FileSupport.ReadFileWithAnyDecoding(settingsFile);
            fontName = settings[0].Split('#').Last();
            fontSize = float.Parse(settings[1].Split('#').Last());
            string textStyle = settings[2].Split('#').Last();
            fontStyle = FileSupport.GetFontStyle(textStyle);
            lengthAccuracy = double.Parse(settings[3].Split('#').Last());
            tempPath = settings[4].Split('#').Last();
            imageParamName = settings[5].Split('#').Last();


            FileSupport.CheckAndDeleteFolder(tempPath);
            Debug.WriteLine("Settings activate success");
            return string.Empty;
        }
    }
}
