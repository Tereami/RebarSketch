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
#region usings
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB;
#endregion

namespace RebarSketch
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        public static string assemblyPath = "";
        public static string rebarSketchPath = "";
        public static string libraryPath = "";
        public static string configFilePath = "";


        public Result OnStartup(UIControlledApplication application)
        {
            ActivatePaths();

            string tabName = "BIM-STARTER TEST";
            try { application.CreateRibbonTab(tabName); } catch { }

            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Rebar sketch");

            PushButton btnCreatePictures = panel1.AddItem(new PushButtonData(
                "CreatePictures",
                "Создать",
                assemblyPath,
                "RebarSketch.CommandCreatePictures3")
               ) as PushButton;

            PushButton btnFormGenerator = panel1.AddItem(new PushButtonData(
                "FormGenerator",
                "Генератор",
                assemblyPath,
                "RebarSketch.CommandFormGenerator")
                ) as PushButton;

            PushButton btnSettings = panel1.AddItem(new PushButtonData(
                "SketchSettings",
                "Настройки",
                assemblyPath,
                "RebarSketch.CommandSettings")
                ) as PushButton;


            //события
            //ControlledApplication ctrlApp = application.ControlledApplication;
            //ctrlApp.DocumentSaving += new EventHandler<DocumentSavingEventArgs>(SavingDocumentEventHandler);


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public static void ActivatePaths()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new RbsLogger.Logger("RebarSketch"));

            assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            string appdataFolder =
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string bimstarterRootFolder =
                System.IO.Path.Combine(appdataFolder, "bim-starter");
            if (!System.IO.Directory.Exists(bimstarterRootFolder))
            {
                Trace.WriteLine("Create folder: " + bimstarterRootFolder);
                System.IO.Directory.CreateDirectory(bimstarterRootFolder);
            }
            configFilePath = Path.Combine(bimstarterRootFolder, "config.ini");

            string bimstarterStoragePath = string.Empty;
            if (File.Exists(configFilePath))
            {
                Trace.WriteLine("Read file: " + configFilePath);
                string[] lines = File.ReadAllLines(configFilePath);
                if (lines.Length > 0)
                {
                    bimstarterStoragePath = lines[0];
                    Trace.WriteLine($"Storage path: {bimstarterStoragePath}");
                }
                else
                {
                    try
                    {
                        System.IO.File.Delete(configFilePath);
                        Trace.WriteLine($"File is deleted: {configFilePath}");
                    }
                    catch
                    {
                        Trace.WriteLine($"Invalid file: {configFilePath}");
                        throw new Exception($"Invalid file: {configFilePath}");
                    }
                }
            }

            if(bimstarterStoragePath == string.Empty)
            {
                Trace.WriteLine("First start, show dialog window and select config folder");
                string configDefaultFolder = Path.Combine(appdataFolder, @"Autodesk\Revit\Addins\20xx\BimStarter");
                FormSelectPath form = new FormSelectPath(configFilePath, configDefaultFolder);
                if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK) 
                    return;

                if (form.UseServerPath)
                {
                    bimstarterStoragePath = form.ServerPath;
                }
                else
                {
                    bimstarterStoragePath = configDefaultFolder;
                }
                Trace.WriteLine("Selected user path: " + bimstarterStoragePath);
                File.WriteAllText(configFilePath, bimstarterStoragePath);
                Trace.WriteLine("Success write to file: " + configFilePath);
            }

            rebarSketchPath = Path.Combine(bimstarterStoragePath, "RebarSketch");
            libraryPath = Path.Combine(rebarSketchPath, "library");
            Trace.WriteLine("Library path: " + libraryPath);
            if (!Directory.Exists(libraryPath))
            {
                Trace.WriteLine("Library isnt found");
                TaskDialog.Show("Rebar Sketch", "Library directory isnt found: " + libraryPath);
            }
        }

        /*
        public void SavingDocumentEventHandler(object sender, DocumentSavingEventArgs args)
        {
            Document doc = args.Document;
            var check = CheckRebars(doc);
            if(check.Count > 0)
            {
                TaskDialog.Show("Ошибка", "Обновите ведомость деталей");
                args.Cancel();
            }
        }

        
        public List<Element> CheckRebars(Document doc)
        {
            List<ParameterElement> paramsCheck = new FilteredElementCollector(doc)
                .OfClass(typeof(ParameterElement))
                .Cast<ParameterElement>()
                .Where(p => p.Name == "RebarImage")
                .ToList();
            if (paramsCheck.Count == 0) return null;

            List<Element> rebars = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategoryId(new ElementId(BuiltInCategory.OST_Rebar))
                .ToList();

            List<Element> errorRebars = new List<Element>();

            foreach (Element rebar in rebars)
            {
                Parameter imageParam = rebar.LookupParameter("RebarImage");
                if (imageParam == null) continue;
                if (imageParam.HasValue == false) continue;
                bool isCorrect = true;

                string line1 = imageParam.AsValueString();
                string line = line1.Substring(0, line1.Length - 4);
                string[] data = line.Split('#');
                if (data.Length < 2) continue;

                string scheduleId = data[0];
                for (int i = 1; i < data.Length; i++)
                {
                    string paramBlock = data[i];
                    if(!paramBlock.Contains("~"))
                    {
                        isCorrect = false;
                        break;
                    }

                    string val = paramBlock.Split('~').Last();
                    double length1 = double.Parse(val);

                    string paramName = paramBlock.Split('~').First();
                    Parameter param = rebar.LookupParameter(paramName);
                    if(param == null)
                    {
                        isCorrect = false;
                        break;
                    }

                    double l = param.AsDouble() * 304.8;
                    double length2 = SupportSettings.lengthAccuracy * Math.Round(l / SupportSettings.lengthAccuracy);

                    if(length1 != length2)
                    {
                        isCorrect = false;
                        break;
                    }
                    
                }

                if (!isCorrect) errorRebars.Add(rebar);
            }

            return errorRebars;
        }
        */
    }
}
