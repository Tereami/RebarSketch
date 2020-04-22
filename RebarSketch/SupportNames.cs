using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RebarSketch
{
    public static class SupportNames
    {
        public static string GetElementName(Element elem)
        {
            string selElemName = "";

            Parameter familyParam = elem.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM);
            if (elem is Rebar)
            {
                selElemName = elem.Name + ", ID: " + elem.Id.IntegerValue.ToString();
            }
            else
            {
                string famName = familyParam.AsValueString();
                if (string.IsNullOrEmpty(famName))
                {
                    selElemName = elem.Name + ", ID: " + elem.Id.IntegerValue.ToString();
                }
                else
                {
                    selElemName = familyParam.AsValueString() + ", ID: " + elem.Id.IntegerValue.ToString();
                }
            }

            return selElemName;
        }
    }
}
