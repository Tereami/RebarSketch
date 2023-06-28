using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RebarSketch
{
    public static class Extensions
    {
        /// <summary>
        /// Получает имя номера формы для стандартной или IFC-арматуры
        /// </summary>
        /// <param name="rebar"></param>
        /// <returns></returns>
        public static string GetRebarFormName(this Element rebar)
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
#else
                List<ElementId> shapeIds = r.GetAllRebarShapeIds().ToList();
                if (shapeIds == null) return "INVALID SHAPE";
                if (shapeIds.Count == 0) return "NO SHAPES";
                if (shapeIds.Count > 1) throw new Exception("Supports only freeformrebar with a single shape");
                ElementId shapeId = shapeIds[0];
                Element shape = doc.GetElement(shapeId);
#endif

                familyName = shape.Name;
            }
            else if (rebar is Autodesk.Revit.DB.Structure.RebarInSystem)
            {
                var r = rebar as Autodesk.Revit.DB.Structure.RebarInSystem;
                Element shape = doc.GetElement(r.RebarShapeId);
                familyName = shape.Name;
            }
            else if (rebar is FamilyInstance)
            {
                FamilyInstance fi = rebar as FamilyInstance;
                familyName = fi.Symbol.FamilyName;
            }
            else
            {
                familyName = rebar.Name;
            }
            return familyName;
        }


        /// <summary>
        /// Проверяет, является ли арматура стандартной или переменной длины
        /// </summary>
        /// <param name="rebar"></param>
        /// <returns>1 - переменная длина, 0 стандартный, -1 ошибка</returns>
        public static int IsVariableLength(this Element rebar)
        {
            Document doc = rebar.Document;

            Guid variableLengthParamGuid = new Guid("ee8d35b0-e2d7-47b3-8b8a-adb31eedac30");
            Parameter variableLengthParam = rebar.get_Parameter(variableLengthParamGuid);

            if (variableLengthParam == null)
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
                    variableLengthParam = rebarType.get_Parameter(variableLengthParamGuid);
                }
                catch { return 0; }
            }
            //else
            //{
            //    //variableLengthParam = parameters.First();
            //}

            if (variableLengthParam == null) return 0;

            int checkIsVariable = variableLengthParam.AsInteger();
            return checkIsVariable;
        }

        /// <summary>
        /// Возвращает человекочитаемое имя элемента
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        public static string GetElementName(this Element elem)
        {
            string selElemName = "";

            Parameter familyParam = elem.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM);
            if (elem is Autodesk.Revit.DB.Structure.Rebar)
            {
                selElemName = $"{elem.Name}, ID: {elem.GetElementId()}";
            }
            else
            {
                string famName = familyParam.AsValueString();
                if (string.IsNullOrEmpty(famName))
                {
                    selElemName = $"{elem.Name}, ID: {elem.GetElementId()}";
                }
                else
                {
                    selElemName = $"{familyParam.AsValueString()}, ID: {elem.GetElementId()}";
                }
            }

            return selElemName;
        }


        public static double GetDoubleValue(this Element rebar, string paramName, out bool isDegrees)
        {
            Parameter param = rebar.LookupParameter(paramName);
            if (param == null)
            {
                ElementType rebarType = rebar.Document.GetElement(rebar.GetTypeId()) as ElementType;
                if (rebarType == null)
                    throw new Exception($"Rebar type is null for element {rebar.GetElementId()}");

                param = rebarType.LookupParameter(paramName);
            }

            if (param == null || !param.HasValue)
            {
                string msg = $"{MyStrings.Parameter} {paramName} {MyStrings.NotFound} {rebar.GetElementName()} {MyStrings.MaybeUpdateFamily}.";
                Autodesk.Revit.UI.TaskDialog.Show(MyStrings.Error, msg);
                System.Diagnostics.Debug.WriteLine(msg);
                throw new Exception(msg);
            }
            double val = param.AsDouble();


#if R2017 || R2018 || R2019 || R2020 || R2021
            double val2 = UnitUtils.ConvertFromInternalUnits(val, param.DisplayUnitType);
            isDegrees = param.Definition.UnitType == UnitType.UT_Angle; // .DisplayUnitType == DisplayUnitType.DUT_DECIMAL_DEGREES;
#else
            double val2 = UnitUtils.ConvertFromInternalUnits(param.AsDouble(), param.GetUnitTypeId());
            ForgeTypeId forgeDataType = param.Definition.GetDataType();
            isDegrees = forgeDataType == SpecTypeId.Angle;
#endif

            return val2;
        }

        public static long GetElementId(this Element elem)
        {
#if R2017 || R2018 || R2019 || R2020 || R2021 || R2022 || R2023
            return elem.Id.IntegerValue;
#else
            return elem.Id.Value;
#endif
        }
    }
}
