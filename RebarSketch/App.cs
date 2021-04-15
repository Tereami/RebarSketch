using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB;

namespace RebarSketch
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {

            SupportSettings.assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            string tabName = "Weandrevit";
            try { application.CreateRibbonTab(tabName); } catch { }

            RibbonPanel panel1 = application.CreateRibbonPanel(tabName, "Ведомость деталей");

            PushButton btnCreatePictures = panel1.AddItem(new PushButtonData(
                "CreatePictures",
                "Создать",
                SupportSettings.assemblyPath,
                "RebarSketch.CommandCreatePictures3")
               ) as PushButton;

            PushButton btnFormGenerator = panel1.AddItem(new PushButtonData(
                "FormGenerator",
                "Генератор",
                SupportSettings.assemblyPath,
                "RebarSketch.CommandFormGenerator")
                ) as PushButton;


            //события
            ControlledApplication ctrlApp = application.ControlledApplication;
            ctrlApp.DocumentSaving += new EventHandler<DocumentSavingEventArgs>(SavingDocumentEventHandler);


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

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
    }
}
