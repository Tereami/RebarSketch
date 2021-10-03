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
        public List<ScetchTemplate> templates;



        public static void SearchAndApplyScetch(
            Dictionary<string, ScetchImage> imagesBase, 
            Element rebar,
            ScetchTemplate st,
            string imagesPrefix,
            GlobalSettings sets)
        {
            Debug.WriteLine("Try to apply scetch for rebar id" + rebar.Id.IntegerValue.ToString());
            Document doc = rebar.Document;
            string imageParamName = sets.imageParamName;
            ScetchImage si = new ScetchImage(rebar, st);
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



        private ScetchTemplate CreateTemplate(string nameFolder, bool AsSubtype)
        {
            string name = nameFolder.Split('\\').Last();
            ScetchTemplate st = new ScetchTemplate();
            st.formName = name;

            if (AsSubtype)
            {
                st.IsSubtype = true;
                string subtypeNumberString = name.Split('_').Last();
                int subtypeNumber = int.Parse(subtypeNumberString);
                st.SubtypeNumber = subtypeNumber;
            }

            string familiesNamesFile = Path.Combine(nameFolder, "families.txt");
            string fileCheck = CheckFileExists(familiesNamesFile);
            if (fileCheck != "")
            {
                Autodesk.Revit.UI.TaskDialog.Show("Ошибка", fileCheck);
                return null;
            }

            string[] familiesNames = FileSupport.ReadFileWithAnyDecoding(familiesNamesFile);
            st.familyNames = familiesNames.ToList();

            string imageFile = Path.Combine(nameFolder, "scetch.png");
            fileCheck = CheckFileExists(imageFile);
            if (fileCheck != "")
            {
                Autodesk.Revit.UI.TaskDialog.Show("Ошибка", fileCheck);
                return null;
            }
            st.templateImagePath = imageFile;

            string paramsFile = Path.Combine(nameFolder, "parameters.txt");
            fileCheck = CheckFileExists(paramsFile);
            if (fileCheck != "")
            {
                Autodesk.Revit.UI.TaskDialog.Show("Ошибка", fileCheck);
                return null;
            }
            string[] paramsArray = FileSupport.ReadFileWithAnyDecoding(paramsFile);
            st.parameters = new List<ScetchParameter>();
            for(int i = 0; i < paramsArray.Length; i++)
            {
                string p = paramsArray[i];
                if (p.StartsWith("#")) continue;
                ScetchParameter sp = new ScetchParameter();
                string[] paramInfo = p.Split(',');
                if (paramInfo.Length < 4)
                {
                    continue;
                }
                sp.Name = paramInfo[0];
                bool checkParseX = float.TryParse(paramInfo[1], out sp.PositionX);
                bool checkParseY = float.TryParse(paramInfo[2], out sp.PositionY);
                bool checkParseR = float.TryParse(paramInfo[3], out sp.Rotation);
                if (!checkParseX || !checkParseY || !checkParseR)
                {
                    throw new ArgumentException("Incorrect syntax in file " + paramsFile.Replace("\\", " \\") + ", line " + i);
                }

                sp.IsNarrow = false;
                if (paramInfo.Length > 4)
                {
                    if (paramInfo[4] == "1")
                    {
                        sp.IsNarrow = true;
                    }
                }

                st.parameters.Add(sp);
            }

            Debug.WriteLine("ScetchTemplate is created");
            return st;
        }


        public void Activate(string libraryPath)
        {
            Debug.WriteLine("Scetch library activation start");
            templates = new List<ScetchTemplate>();
            string[] nameFolders = Directory.GetDirectories(libraryPath);
            Debug.WriteLine("Folders found: " + nameFolders.Length.ToString());
            foreach (string nameFolder in nameFolders)
            {
                Debug.WriteLine("Check folder: " + nameFolder);
                string[] subFolders = System.IO.Directory.GetDirectories(nameFolder);
                if (subFolders.Length == 0)
                {
                    Debug.WriteLine("No subfolders, create scetch template");
                    ScetchTemplate st = CreateTemplate(nameFolder, false);
                    if (st == null)
                    {
                        Debug.WriteLine("Scetch is null");
                        continue;
                    }
                    templates.Add(st);
                    Debug.WriteLine("Scetch succesfuly added to library as form name: " + st.formName);
                }
                else
                {
                    Debug.WriteLine("Subfolders found");
                    foreach (string subfolder in subFolders)
                    {
                        Debug.WriteLine("Create template by subfolder: " + subfolder);
                        ScetchTemplate st2 = CreateTemplate(subfolder, true);
                        if (st2 == null)
                        {
                            Debug.WriteLine("Scetch is null");
                            continue;
                        }
                        templates.Add(st2);
                        Debug.WriteLine("Scetch succesfuly added to library as form name: " + st2.formName);
                    }
                }
            }
            Debug.WriteLine("Scetch library activation start");
        }

        private string CheckFileExists(string path)
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
        }

        public ScetchTemplate GetTemlateByFamilyName(string familyName, Element rebar)
        {
            Debug.WriteLine("Get template by family name: " + familyName + " for element id " + rebar.Id.IntegerValue.ToString());
            foreach (ScetchTemplate st in templates)
            {
                foreach (string name in st.familyNames)
                {
                    if (name == familyName)
                    {

                        if (st.IsSubtype)
                        {
                            Parameter subtypeNumberParam = rebar.LookupParameter("Арм.НомерПодтипаФормы");
                            if (subtypeNumberParam == null) return null;

                            int subtypeNumber = subtypeNumberParam.AsInteger();
                            if (st.SubtypeNumber == subtypeNumber)
                            {
                                Debug.WriteLine("Scetch template as Subtype " + subtypeNumber.ToString());
                                return st;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Scetch template found, not subtype");
                            return st;
                        }
                    }
                }
            }
            return null;
        }
    }
}
