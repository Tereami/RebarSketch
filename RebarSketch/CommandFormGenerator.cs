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
            Form1 form = new Form1(sets);

            form.ShowDialog();

            return Result.Succeeded;
        }
    }
}
