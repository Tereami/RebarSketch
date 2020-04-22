using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Autodesk.Revit.DB;

namespace RebarSketch
{
    public class ScetchTemplate
    {
        public string formName;
        public List<string> familyNames;
        public string templateImagePath;
        public Bitmap templateImage;
        public List<ScetchParameter> parameters;

        public bool IsSubtype;
        public int SubtypeNumber;



        public ScetchTemplate()
        {
            IsSubtype = false;
            SubtypeNumber = 0;
        }

        /// <summary>
        /// Получает имя номера формы для стандартной или IFC-арматуры
        /// </summary>
        /// <param name="rebar"></param>
        /// <returns></returns>
        public static string GetFormNameByElement(Element rebar)
        {
            string familyName = "";
            Document doc = rebar.Document;
            if (rebar is Autodesk.Revit.DB.Structure.Rebar)
            {
                Autodesk.Revit.DB.Structure.Rebar r = rebar as Autodesk.Revit.DB.Structure.Rebar;
#if R2017
                Element shape = doc.GetElement(r.RebarShapeId);
#elif R2018
                Element shape = doc.GetElement(r.GetShapeId());
#elif R2019
                Element shape = doc.GetElement(r.GetShapeId());
#elif R2020
                Element shape = doc.GetElement(r.GetShapeId());
#elif R2021
                Element shape = doc.GetElement(r.GetShapeId());
#endif
                familyName = shape.Name;
            }
            else if (rebar is FamilyInstance)
            {
                FamilyInstance fi = rebar as FamilyInstance;
                familyName = fi.Symbol.FamilyName;
            }
            return familyName;
        }

        /// <summary>
        /// Проверяется, является ли арматура стандартной или переменной длины
        /// </summary>
        /// <param name="rebar"></param>
        /// <returns>1 - переменная длина, 0 стандартный, -1 ошибка</returns>
        public static int CheckrebarIsVariableLength(Element rebar)
        {
            Document doc = rebar.Document;
            //Guid variableLengthParamGuid = new Guid("ee8d35b0-e2d7-47b3-8b8a-adb31eedac30");
            //List<Parameter> parameters = rebar.GetOrderedParameters()
            //    .Where(p => p.IsShared)
            //    .Where(p => p.GUID == variableLengthParamGuid)
            //    .ToList();

            Parameter variableLengthParam = rebar.LookupParameter("Рзм.ПеременнаяДлина");

            if (variableLengthParam  == null)
            {
                try
                {
                    ElementId typeId = rebar.GetTypeId();
                    Element rebarType = doc.GetElement(typeId);
                    //List<Parameter> typeParameters = rebar.GetOrderedParameters()
                    //    .Where(p => p.IsShared)
                    //    .Where(p => p.GUID == variableLengthParamGuid)
                    //    .ToList();
                    //if (typeParameters.Count == 0) return -1;

                    //variableLengthParam = typeParameters.First();
                    variableLengthParam = rebarType.LookupParameter("Рзм.ПеременнаяДлина");
                }
                catch { return -1; }
            }
            //else
            //{
            //    //variableLengthParam = parameters.First();
            //}

            if (variableLengthParam == null) return -1;

            int checkIsVariable = variableLengthParam.AsInteger();
            return checkIsVariable;
        }
    }
}
