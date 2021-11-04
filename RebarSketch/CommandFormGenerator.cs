using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RebarSketch
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]

    class CommandFormGenerator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            App.ActivatePaths();

            GlobalSettings sets = GlobalSettings.Read();

            Document doc = commandData.Application.ActiveUIDocument.Document;

            ScetchLibrary lib = new ScetchLibrary();
            lib.Activate(App.libraryPath);
            List<XmlSketchItem> oldFormatTemplates = lib.templates.Where(i => i.IsXmlSource == false).ToList();
            if (oldFormatTemplates.Count > 0)
            {
                TaskDialog.Show("Инфо", "Библиотека эскизов будет обновлена до нового формата");
                foreach(XmlSketchItem xsi in lib.templates)
                {
                    xsi.Save();
                }
            }

            Form1 form = new Form1(sets);

            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}
