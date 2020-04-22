using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Autodesk.Revit.DB;

namespace RebarSketch
{
    public class ScetchLibrary
    {
        public string FontName;
        public string LibraryPath;
        public List<ScetchTemplate> templates;



        public static void SearchAndApplyScetch(Dictionary<string, ScetchImage> imagesBase, Element rebar, ScetchTemplate st, string imagesPrefix)
        {
            Document doc = rebar.Document;
            string imageParamName = SupportSettings.imageParamName;
            ScetchImage si = new ScetchImage(rebar, st);
            ImageType imType2 = null;


            if (imagesBase.ContainsKey(si.ImageKey)) //такая картинка уже ранее генерировалась и есть в проекте
            {
                var baseImage = imagesBase[si.ImageKey];
                rebar.LookupParameter(imageParamName).Set(baseImage.imageType.Id);
            }
            else //такая картинка еще не генерировалась - генерируем, добавляем в базу
            {
                si.Generate(imagesPrefix);
                imType2 = ImageType.Create(doc, si.ScetchImagePath);
                rebar.LookupParameter(imageParamName).Set(imType2.Id);
                si.imageType = imType2;
                imagesBase.Add(si.ImageKey, si);
            }
        }



        private ScetchTemplate CreateTemplate(string nameFolder, bool AsSubtype)
        {
            string name = nameFolder.Split('\\').Last();
            ScetchTemplate st = new ScetchTemplate();
            st.formName = name;

            if(AsSubtype)
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
            foreach (string p in paramsArray)
            {
                if (p.StartsWith("#")) continue;
                ScetchParameter sp = new ScetchParameter();
                string[] paramInfo = p.Split(',');
                sp.Name = paramInfo[0];
                sp.PositionX = int.Parse(paramInfo[1]);
                sp.PositionY = int.Parse(paramInfo[2]);
                sp.Rotation = int.Parse(paramInfo[3]);

                sp.NeedsWrap = false;
                if (paramInfo.Length > 4)
                {
                    if (paramInfo[4] == "1")
                    {
                        sp.NeedsWrap = true;
                    }
                }

                st.parameters.Add(sp);
            }

            return st;
        }


        public void Activate(string libraryPath)
        {
            templates = new List<ScetchTemplate>();
            string[] nameFolders = Directory.GetDirectories(libraryPath);
            foreach (string nameFolder in nameFolders)
            {
                string[] subFolders = System.IO.Directory.GetDirectories(nameFolder);
                if (subFolders.Length == 0)
                {
                    ScetchTemplate st = CreateTemplate(nameFolder, false);
                    if (st == null) continue;
                    templates.Add(st);
                }
                else
                {
                    foreach(string subfolder in subFolders)
                    {
                        ScetchTemplate st2 = CreateTemplate(subfolder, true);
                        if (st2 == null) continue;
                        templates.Add(st2);
                    }
                }
            }
        }

        private string CheckFileExists(string path)
        {
            bool check = File.Exists(path);
            if (!check)
            {
                string res = "Не найден файл: " + path;
                res = res.Replace("\\", " \\ ");
                return res;
            }
            return "";
        }

        public ScetchTemplate GetTemlateByFamilyName(string familyName, Element rebar)
        {
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
                            if(st.SubtypeNumber == subtypeNumber)
                            {
                                return st;
                            }
                        }
                        else
                        {
                            return st;
                        }
                    }
                }
            }
            return null;
        }
    }
}
