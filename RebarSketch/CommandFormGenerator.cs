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

            

            Document doc = commandData.Application.ActiveUIDocument.Document;

            ScetchLibrary lib = new ScetchLibrary();
            lib.Activate(App.libraryPath);
            List<XmlSketchItem> oldFormatTemplates = lib.templates.Where(i => i.IsXmlSource == false).ToList();
            if (oldFormatTemplates.Count > 0)
            {
                TaskDialog.Show(MyStrings.Info, MyStrings.MessageLibraryWillBeUpdated);
                foreach(XmlSketchItem xsi in lib.templates)
                {
                    xsi.Save();
                }
            }

            string librarypath = App.libraryPath;
            Form1 form = new Form1(librarypath);

            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}
