using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RebarSketch
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class CommandCreatePictures3 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            App.ActivatePaths();
            Debug.WriteLine("Start rebar sketch, revit version" + commandData.Application.Application.VersionName);

            Document doc = commandData.Application.ActiveUIDocument.Document;

            Debug.WriteLine("Read settings");
            //считываем файл настроек
            GlobalSettings sets = GlobalSettings.Read();
            
            //выбираем арматуру, которую будем обрабатывать
            Autodesk.Revit.UI.Selection.Selection sel = commandData.Application.ActiveUIDocument.Selection;
            List<ElementId> selIds = sel.GetElementIds().ToList();
            //bool checkSelectionRebar = true;
            if (selIds.Count == 0)
            {
                message = "Перед запуском перейдите в Ведомость деталей, выберите все строчки и после этого запускайте плагин.";
                Debug.WriteLine("No selected elements");
                return Result.Failed;
            }


            if (selIds.Count > 0)
            {
                ElementId selId = selIds.First();
                Element selElem = doc.GetElement(selId);
                if (selElem.Category.Id.IntegerValue != new ElementId(BuiltInCategory.OST_Rebar).IntegerValue)
                {
                    message = "Перед запуском перейдите в Ведомость деталей, выберите все строчки и после этого запускайте плагин.";
                    Debug.WriteLine("No selected rebar elements");
                    return Result.Failed;
                }
            }

            List<Element> col = new List<Element>();
            foreach (ElementId rebarId in selIds)
            {
                Element elem = doc.GetElement(rebarId);
                col.Add(elem);
            }

            View activeView = commandData.Application.ActiveUIDocument.ActiveView;
            ViewSchedule vs = activeView as ViewSchedule;
            if (vs == null)
            {
                message = "Перед запуском перейдите в Ведомость деталей, выберите все строчки и после этого запускайте плагин.";
                Debug.WriteLine("Active view is not ViewSchedule");
                return Result.Failed;
            }

            //очищаю ранее созданные картинки для данной ведомости деталей
            string imagesPrefix = vs.Id.IntegerValue.ToString();
            List<ElementId> oldImageIds = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(ImageType))
                .Where(i => i.Name.StartsWith(imagesPrefix))
                .Select(i => i.Id)
                .ToList();
            Debug.WriteLine("Old scetch images found: " + oldImageIds.Count.ToString());

            if (oldImageIds.Count > 0)
            {
                using (Transaction t1 = new Transaction(doc))
                {
                    t1.Start("Очистка");
                    doc.Delete(oldImageIds);
                    t1.Commit();
                }
            }

            ScetchLibrary lib = new ScetchLibrary();
            lib.Activate(App.libraryPath);
            if(lib.templates.Count == 0)
            {
                message = "Библиотека пуста";
                return Result.Failed;
            }
            List<XmlSketchItem> oldFormatTemplates = lib.templates.Where(i => i.IsXmlSource == false).ToList();
            if(oldFormatTemplates.Count > 0)
            {
                message = "Библиотека не обновлена до нового формата. Запустите Конструктор форм или обратитесь в bim-отдел";
                return Result.Failed;
            }
                    

            System.IO.Directory.CreateDirectory(sets.tempPath);
            Debug.WriteLine("Create temp folder: " + sets.tempPath);


            //разделяю арматуру на обычную и переменной длины
            List<Element> standartRebars = new List<Element>();
            List<Element> variableRebars = new List<Element>();
            foreach (Element rebar in col)
            {
                int checkIsVariable = rebar.IsVariableLength();
                if (checkIsVariable == -1) continue;

                if (checkIsVariable == 0)
                    standartRebars.Add(rebar);
                else
                    variableRebars.Add(rebar);
            }

            Debug.WriteLine("Standart rebars: " + standartRebars.Count.ToString() + ", variable rebars: " + variableRebars.Count.ToString());


            //группировка арматуры переменной длины по марке
            Dictionary<string, List<Element>> variableRebarBase = new Dictionary<string, List<Element>>();
            foreach (Element vRebar in variableRebars)
            {
                string mark = vRebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString();
                if (mark == null)
                {
                    string msg = "Обнаружены арматурные стержни переменной длины, для которых не назначена Марки. ";
                    msg += "Группировка для таких стержней выполняется по Марке, которую нужно назначить заранее.";
                    Debug.WriteLine("Non-marked variable rebars is found");
                    TaskDialog.Show("Ошибка", msg);
                    return Result.Failed;
                }

                if (variableRebarBase.ContainsKey(mark))
                    variableRebarBase[mark].Add(vRebar);
                else
                    variableRebarBase.Add(mark, new List<Element> { vRebar });
            }

            Dictionary<string, ScetchImage> imagesBase = new Dictionary<string, ScetchImage>();

            HashSet<string> errorRebarNames = new HashSet<string>();

            using (Transaction t2 = new Transaction(doc))
            {
                t2.Start("Ведомость деталей");

                //заполняю картинки для обычной арматуры
                foreach (Element rebar in standartRebars)
                {
                    string formName = rebar.GetRebarFormName();
                    if (formName == "") continue;

                    XmlSketchItem xsi = lib.FindTemplate(rebar);
                    if (xsi == null)
                    {
                        errorRebarNames.Add(formName);
                        continue;
                        //return Result.Failed;
                    }

                    foreach (ScetchParameter sparam in xsi.parameters)
                    {
                        string paramName = sparam.Name;

                        Parameter lengthParam = rebar.LookupParameter(paramName);
                        if (lengthParam == null)
                        {
                            message = "Параметр " + paramName + " не найден в " + rebar.GetElementName()
                                + ". Возможно, нужно обновить семейство.";
                            Debug.WriteLine(message);
                            return Result.Failed;
                        }

                        string textVal = lengthParam.Definition.Name;
                        double val = lengthParam.AsDouble();


#if R2022
                        ForgeTypeId forgeType = lengthParam.GetUnitTypeId();
                        string unittype = forgeType.TypeId;
                        bool isMillimeters = unittype.Contains("millimeters");
                        bool isDegrees = unittype.Contains("degrees");
#else
                        bool isMillimeters = lengthParam.DisplayUnitType == DisplayUnitType.DUT_MILLIMETERS;
                        bool isDegrees = lengthParam.DisplayUnitType == DisplayUnitType.DUT_DECIMAL_DEGREES;
#endif
                        if (isDegrees)
                        {
                            val = SupportMath.RoundDegrees(val);
                            textVal = val.ToString("F0") + "°";
                            sparam.value = textVal;
                        }
                        else
                        {
                            val = SupportMath.RoundMillimeters(val, sparam.MinValueForRound, sparam.LengthAccuracy);
                            textVal = val.ToString("F0");
                            sparam.value = textVal;
                        }

                        Debug.WriteLine("ScetchParameter name " + sparam.Name + " value = " + textVal);
                    }

                    ScetchLibrary.SearchAndApplyScetch(imagesBase, rebar, xsi, imagesPrefix, sets);
                }


                //заполняю картинки для арматуры переменной длины
                foreach (var kvp in variableRebarBase)
                {
                    string mark = kvp.Key;
                    List<Element> rebars = kvp.Value;

                    string formName = rebars.First().GetRebarFormName();
                    XmlSketchItem xsi = lib.FindTemplate(rebars.First());
                    if (xsi == null)
                    {
                        errorRebarNames.Add(formName);
                        continue;
                    }

                    //получаю для каждого имени параметра список его возможных значений
                    Dictionary<string, HashSet<double>> variableValues = new Dictionary<string, HashSet<double>>();
                    foreach (Element rebar in rebars)
                    {
                        foreach (ScetchParameter sparam in xsi.parameters)
                        {
                            string paramName = sparam.Name;
                            Parameter lengthParam = rebar.LookupParameter(paramName);
                            if (lengthParam == null)
                            {
                                message = "Параметр " + paramName + " не найден в " +  rebar.GetRebarFormName()
                                    + ". Возможно, попытка свести в одну позицию стержни разной формы. "
                                    + ". При использовании арматуры \"Переменной длины\" следует вручную назначить разные \"Марки\" для стержней разных позиций.";
                                Debug.WriteLine("No found parameter " + paramName + " in element " + rebar.Id.IntegerValue.ToString());
                                return Result.Failed;
                            }
                            double val = rebar.LookupParameter(paramName).AsDouble();
                            if (variableValues.ContainsKey(paramName))
                            {
                                variableValues[paramName].Add(val);
                                
                            }
                            else
                            {
                                variableValues.Add(paramName, new HashSet<double> { val });
                            }
                            Debug.WriteLine("Add variableValues " + paramName + " = " + val.ToString());
                        }
                    }

                    foreach (ScetchParameter sparam in xsi.parameters)
                    {
                        string paramName = sparam.Name;
                        HashSet<double> values = variableValues[paramName];
                        int count = values.Count();
                        double minValue = values.Min();
                        minValue = SupportMath.RoundMillimeters(minValue, sparam.MinValueForRound, sparam.LengthAccuracy);
                        double maxValue = values.Max();
                        maxValue = SupportMath.RoundMillimeters(maxValue, sparam.MinValueForRound, sparam.LengthAccuracy);
                        double spacing = (maxValue - minValue) / (count - 1);
                        spacing = sparam.LengthAccuracy * Math.Round(spacing / sparam.LengthAccuracy); //Math.Round(spacing, 0);

                        //string line = "";

                        if (minValue == maxValue || count == 1)
                        {
                            sparam.value = minValue.ToString("F0");
                            sparam.IsVariable = false;
                            sparam.HaveSpacing = false;
                        }
                        else
                        {
                            if (count == 2)
                            {
                                sparam.value = minValue.ToString("F0") + "..." + maxValue.ToString("F0");
                                sparam.IsVariable = true;
                                sparam.HaveSpacing = false;
                            }
                            else
                            {
                                //sparam.value = minValue.ToString("F0") + "..." + maxValue.ToString("F0") + " (ш." + spacing.ToString("F0") + ")";
                                sparam.value = minValue.ToString("F0") + "..." + maxValue.ToString("F0");
                                sparam.HaveSpacing = true;
                                sparam.IsVariable = true;
                                sparam.SpacingValue = "ш." + spacing.ToString("F0");
                            }
                        }
                    }

                    foreach (Element rebar in rebars)
                    {
                        Debug.WriteLine("Processed rebar id " + rebar.Id.IntegerValue.ToString());
                        ScetchImage si = new ScetchImage(rebar, xsi);

                        ScetchLibrary.SearchAndApplyScetch(imagesBase, rebar, xsi, imagesPrefix, sets);
                    }
                }
                t2.Commit();
            }

            FileSupport.CheckAndDeleteFolder(sets.tempPath);

            if (errorRebarNames.Count > 0)
            {
                string errorFamilyMessage = "Не удалось обработать семейства. Скорее всего, применены семейства не из библиотеки семейства. Имена семейств: ";
                foreach (string fam in errorRebarNames)
                {
                    errorFamilyMessage = errorFamilyMessage + fam + "; ";
                }
                Debug.WriteLine(errorFamilyMessage);
                TaskDialog.Show("Отчет", errorFamilyMessage);
            }

            Debug.WriteLine("Scetches finish success");
            return Result.Succeeded;
        }
    }
}
