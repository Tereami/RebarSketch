﻿#region License
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
#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
#endregion

namespace RebarSketch
{
    [Serializable]
    public class XmlSketchItem
    {
        public List<string> families { get; set; }
        public List<ScetchParameter> parameters { get; set; }

        public XmlSketchItem()
        {
            //пустой конструктор для сериализатора
        }

        public XmlSketchItem(ScetchTemplate template)
        {
            families = template.familyNames;

            parameters = template.parameters;
        }

        public static XmlSketchItem LoadFromXml(string xmlPath)
        {
            Debug.WriteLine("Read Xml sketch file: " + xmlPath);
            XmlSketchItem xsi;
            XmlSerializer serializer = new XmlSerializer(typeof(XmlSketchItem));

            using (StreamReader reader = new StreamReader(xmlPath))
            {
                xsi = (XmlSketchItem)serializer.Deserialize(reader);
                if (xsi == null)
                {
                    System.Windows.Forms.MessageBox.Show("Не удалось загрузить настройки, установлены по-умолчанию");
                    Debug.WriteLine("Unable to get setiings, set default");
                    xsi = new XmlSketchItem();
                }
            }

            return xsi;
        }

        public static XmlSketchItem LoadFromTxt(string folder)
        {
            XmlSketchItem xsi = new XmlSketchItem();

            string parametersTxtPath = Path.Combine(folder, "parameters.txt");

            if (!File.Exists(parametersTxtPath))
                throw new Exception("File not found " + parametersTxtPath);


            string[] paramsArray = FileSupport.ReadFileWithAnyDecoding(parametersTxtPath);

            xsi.parameters = new List<ScetchParameter>();
            for (int i = 0; i < paramsArray.Length; i++)
            {
                string p = paramsArray[i];
                if (p.StartsWith("#")) continue;
                if (p.Length < 1) continue;
                string[] paramInfo = p.Split(',');
                if (paramInfo.Length < 4)
                {
                    throw new Exception("Incorrect syntax in file " + parametersTxtPath + ", line " + i);
                }
                ScetchParameter sparam = new ScetchParameter();
                sparam.Name = paramInfo[0];
                sparam.PositionX = float.Parse(paramInfo[1]);
                sparam.PositionY = float.Parse(paramInfo[2]);
                sparam.Rotation = float.Parse(paramInfo[3]);

                if (paramInfo.Length > 4)
                {
                    if (paramInfo[4] == "1")
                    {
                        sparam.IsNarrow = true;
                    }
                }
                xsi.parameters.Add(sparam);
            }

            xsi.families = new List<string>();
            string familiesTxtPath = System.IO.Path.Combine(folder, "families.txt");
            if (!System.IO.File.Exists(familiesTxtPath))
                throw new Exception("File not found " + familiesTxtPath);

            string[] familiesArray = FileSupport.ReadFileWithAnyDecoding(familiesTxtPath);
            xsi.families = familiesArray.ToList();

            return xsi;
        }

        public static void Save(string folder, XmlSketchItem xsi)
        {
            string xmlPath = Path.Combine(folder, "config.xml");
            Debug.WriteLine("Save sketch config to file: " + xmlPath);
            if (File.Exists(xmlPath))
                File.Delete(xmlPath);

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(XmlSketchItem));
                using (FileStream writer = new FileStream(xmlPath, FileMode.OpenOrCreate))
                {
                    serializer.Serialize(writer, xsi);
                    Debug.WriteLine("Save settings success");
                }
            }
            catch
            {
                Debug.WriteLine("Save settings failed");
                throw new Exception("Error save file " + xmlPath);
            }

            string txtParametersFile = Path.Combine(folder, "parameters.txt");
            if (File.Exists(txtParametersFile))
            {
                File.Move(txtParametersFile, txtParametersFile + "old");
                Debug.WriteLine("File deleted: " + txtParametersFile);
            }

            string txtFamiliesFile = Path.Combine(folder, "families.txt");
            if (File.Exists(txtFamiliesFile))
            {
                File.Move(txtFamiliesFile, txtFamiliesFile + "old");
                Debug.WriteLine("File deleted: " + txtFamiliesFile);
            }
        }
    }
}