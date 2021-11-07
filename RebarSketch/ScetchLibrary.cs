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
using Autodesk.Revit.DB;
#endregion

namespace RebarSketch
{
    public class ScetchLibrary
    {
        public string FontName;
        public string LibraryPath;
        public List<XmlSketchItem> templates;



        public static void SearchAndApplyScetch(
            Dictionary<string, ScetchImage> imagesBase,
            Element rebar,
            XmlSketchItem xsi,
            string imagesPrefix,
            GlobalSettings sets)
        {
            Debug.WriteLine("Try to apply scetch for rebar id" + rebar.Id.IntegerValue.ToString());
            Document doc = rebar.Document;
            string imageParamName = sets.imageParamName;
            ScetchImage si = new ScetchImage(rebar, xsi);
            ImageType imType2 = null;

            Debug.WriteLine("Key: " + si.ImageKey);

            if (imagesBase.ContainsKey(si.ImageKey)) //такая картинка уже ранее генерировалась и есть в проекте
            {
                Debug.WriteLine("Scetch exists, get from base");
                var baseImage = imagesBase[si.ImageKey];
                rebar.LookupParameter(imageParamName).Set(baseImage.imageType.Id);
                Debug.WriteLine("Set imagetype id" + baseImage.imageType.Id.IntegerValue.ToString() + " to rebar id" + rebar.Id.IntegerValue.ToString());
            }
            else //такая картинка еще не генерировалась - генерируем, добавляем в базу
            {
                si.Generate(sets, imagesPrefix);
#if R2017 || R2018 || R2019
                imType2 = ImageType.Create(doc, si.ScetchImagePath);
#elif R2020 
                imType2 = ImageType.Create(doc, new ImageTypeOptions(si.ScetchImagePath));
#elif R2021 || R2022
                ImageTypeOptions ito = new ImageTypeOptions(si.ScetchImagePath, false, ImageTypeSource.Import);
                imType2 = ImageType.Create(doc, ito);
#endif
                Debug.WriteLine("Create imagetype id=" + imType2.Id.IntegerValue.ToString());
                Parameter imageparam = rebar.LookupParameter(imageParamName);
                if (imageparam == null)
                {
                    string msg = "Нет параметра " + imageParamName + " в элементе id" + rebar.Id.IntegerValue.ToString();
                    Debug.WriteLine(msg);
                    System.Windows.Forms.MessageBox.Show(msg);
                    throw new Exception(msg);
                }
                if (imageparam.StorageType != StorageType.ElementId)
                {
                    string msg = "Неверный тип параметра " + imageParamName;
                    Debug.WriteLine(msg);
                    System.Windows.Forms.MessageBox.Show(msg);
                    System.Environment.Exit(1);
                }
                imageparam.Set(imType2.Id);
                si.imageType = imType2;
                imagesBase.Add(si.ImageKey, si);
                Debug.WriteLine("Scetch is created, ImageType id=" + imType2.Id.IntegerValue.ToString());
            }
        }


        public void Activate(string libraryPath)
        {
            Debug.WriteLine("Scetch library activation start");
            templates = new List<XmlSketchItem>();
            string[] nameFolders = Directory.GetDirectories(libraryPath);
            Debug.WriteLine("Folders found: " + nameFolders.Length.ToString());
            foreach (string nameFolder in nameFolders)
            {
                Debug.WriteLine("Check folder: " + nameFolder);
                string[] subFolders = System.IO.Directory.GetDirectories(nameFolder);
                if (subFolders.Length == 0)
                {
                    Debug.WriteLine("No subfolders, create scetch template");
                    XmlSketchItem xsi = XmlSketchItem.Load(nameFolder);
                    if (xsi == null)
                    {
                        Debug.WriteLine("Scetch is null");
                        continue;
                    }
                    templates.Add(xsi);
                    Debug.WriteLine("Scetch succesfuly added to library as form name: " + xsi.formName);
                }
                else
                {
                    Debug.WriteLine("Subfolders found");
                    foreach (string subfolder in subFolders)
                    {
                        Debug.WriteLine("Create template by subfolder: " + subfolder);
                        XmlSketchItem xsi2 = XmlSketchItem.Load(subfolder);
                        if (xsi2 == null)
                        {
                            Debug.WriteLine("Scetch is null");
                            continue;
                        }
                        xsi2.IsSubtype = true;
                        string subtypeNumberString = subfolder.Split('_').Last();
                        int subtypeNumber = int.Parse(subtypeNumberString);
                        xsi2.SubtypeNumber = subtypeNumber;
                        templates.Add(xsi2);
                        Debug.WriteLine("Scetch succesfuly added to library as form name: " + xsi2.formName);
                    }
                }
            }
            Debug.WriteLine("Scetch library activation start");
        }

        /*private string CheckFileExists(string path)
        {
            bool check = File.Exists(path);
            if (!check)
            {
                string res = "Не найден файл: " + path;
                res = res.Replace("\\", " \\ ");
                Debug.WriteLine(res);
                return res;
            }
            return "";
        }*/

        public XmlSketchItem FindTemplate(Element rebar)
        {
            string familyName = rebar.GetRebarFormName();
            Debug.WriteLine("Get template by family name: " + familyName + " for element id " + rebar.Id.IntegerValue.ToString());
            List<XmlSketchItem> curNameSketches = templates.Where(i => i.families.Contains(familyName)).ToList();
            if (curNameSketches.Count == 0) return null;

            if (curNameSketches[0].IsSubtype)
            {
                Parameter subtypeNumberParam = rebar.LookupParameter("Арм.НомерПодтипаФормы");
                if (subtypeNumberParam == null)
                {
                    string msg = "Арматура " + familyName + " не содержит параметр Арм.НомерПодтипаФормы";
                    Autodesk.Revit.UI.TaskDialog.Show("Ошибка", msg);
                    throw new Exception(msg);
                }

                int subtypeNumber = subtypeNumberParam.AsInteger();
                List<XmlSketchItem> curSubtypeTemplate = curNameSketches
                    .Where(i => i.SubtypeNumber == subtypeNumber)
                    .ToList();
                if(curSubtypeTemplate.Count == 0)
                {
                    string msg = "Нет шаблона подтипа №" + subtypeNumber + " для арматуры " + familyName;
                    Autodesk.Revit.UI.TaskDialog.Show("Ошибка", msg);
                    throw new Exception(msg);
                }
                else if(curSubtypeTemplate.Count > 1)
                {
                    string msg = "Более 1 шаблона подтипа №" + subtypeNumber + " для арматуры " + familyName;
                    Autodesk.Revit.UI.TaskDialog.Show("Ошибка", msg);
                    throw new Exception(msg);
                }

                return curSubtypeTemplate[0];
            }
            else
            {
                if (curNameSketches.Count > 1)
                {
                    string msg = "Арматура " + familyName + " прописана в нескольких шаблонах: ";
                    msg += string.Join(", ", curNameSketches.Select(i => i.formName));
                    Autodesk.Revit.UI.TaskDialog.Show("Ошибка", msg);
                    throw new Exception(msg);
                }
                Debug.WriteLine("Scetch template found, not subtype");
                return curNameSketches[0];
            }
        }
    }
}
