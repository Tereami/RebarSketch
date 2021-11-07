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
using System.Xml.Serialization;
#endregion

namespace RebarSketch
{
    public class GlobalSettings
    {
        public string tempPath = @"C:\RebarScetch";
        public string fontName = "Isocpeur";
        public float defaultFontSize = 25;
        public double defautLengthAccuracy = 5;

        public float linesSpacing = 1.8f;


        public System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;
        public string imageParamName = "RebarImage";

        public GlobalSettings()
        {
        }
        

        public static bool Save(GlobalSettings ssets)
        {
            string xmlPath = Path.Combine(App.rebarSketchPath, "settings.xml");
            Debug.WriteLine("Save settings to file: " + xmlPath);
            if (File.Exists(xmlPath))
            {
                try
                {
                    File.Delete(xmlPath);
                }
                catch
                {
                    string msg = "Не удалось сохранить файл, проверьте права доступа " + xmlPath;
                    System.Windows.Forms.MessageBox.Show(msg);
                    throw new Exception(msg);
                }
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(GlobalSettings));
                using (FileStream writer = new FileStream(xmlPath, FileMode.OpenOrCreate))
                {
                    serializer.Serialize(writer, ssets);
                }
                return true;
            }
            catch
            {
                Debug.WriteLine("Save settings failed");
                return false;
            }
        }

        public static GlobalSettings Read()
        {
            string rebarSketchPath = App.rebarSketchPath;
            Debug.WriteLine("Read settings from folder " + rebarSketchPath);
            GlobalSettings ssets = null;
            string settingsFileXml = Path.Combine(rebarSketchPath, "settings.xml");
            string settingsFileTxt = Path.Combine(rebarSketchPath, "settings.txt");
            if (File.Exists(settingsFileXml))
            {
                if (File.Exists(settingsFileTxt))
                    try
                    {
                        File.Delete(settingsFileTxt);
                    }
                    catch
                    {
                        Debug.WriteLine("Не удается удалить файл " + settingsFileTxt);
                    }

                ssets = GlobalSettings.ReadFromXml(settingsFileXml);
            }
            else if(File.Exists(settingsFileTxt))
            {
                ssets = GlobalSettings.ReadFromTxt(settingsFileTxt);
            }
            else
            {
                ssets = new GlobalSettings();
            }

            Debug.WriteLine("Settings activate success");

            //на всякий случай очистить временную папку с эскизами
            FileSupport.CheckAndDeleteFolder(ssets.tempPath);

            return ssets;
        }

        public static GlobalSettings ReadFromXml(string xmlPath)
        {
            Debug.WriteLine("Read Xml settings file: " + xmlPath);
            GlobalSettings ssets;
            XmlSerializer serializer = new XmlSerializer(typeof(GlobalSettings));

            using (StreamReader reader = new StreamReader(xmlPath))
            {
                ssets = (GlobalSettings)serializer.Deserialize(reader);
                if (ssets == null)
                {
                    System.Windows.Forms.MessageBox.Show("Не удалось загрузить настройки, установлены по-умолчанию");
                    Debug.WriteLine("Unable to get setiings, set default");
                    ssets = new GlobalSettings();
                }
            }

            return ssets;
        }

        public static GlobalSettings ReadFromTxt(string txtPath)
        {
            Debug.WriteLine("Read txt settings file: " + txtPath);

            GlobalSettings ssets = new GlobalSettings();

            string[] settings = FileSupport.ReadFileWithAnyDecoding(txtPath);
            ssets.fontName = settings[0].Split('#').Last();
            ssets.defaultFontSize = float.Parse(settings[1].Split('#').Last());
            string textStyleName = settings[2].Split('#').Last();
            ssets.fontStyle = FileSupport.GetFontStyle(textStyleName);
            ssets.defautLengthAccuracy = double.Parse(settings[3].Split('#').Last());
            ssets.tempPath= settings[4].Split('#').Last();
            ssets.imageParamName = settings[5].Split('#').Last();

            Debug.WriteLine("Read txt settings success");

            try
            {
                System.IO.File.Delete(txtPath);
                GlobalSettings.Save(ssets);
                Debug.WriteLine("File deleted " + txtPath);
            }
            catch { }
            
            return ssets;
        }
    }
}
