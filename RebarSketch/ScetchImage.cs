using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.Drawing;

namespace RebarSketch
{
    public class ScetchImage : IEquatable<ScetchImage>
    {
        //public List<ScetchParameter> Parameters;
        public ScetchTemplate Template;
        public Element Elem;
        public ImageType imageType;
        public string ImageKey;
        public string ScetchImagePath;

        bool roundForSmallDimension;

        public ScetchImage(Element elem, ScetchTemplate template)
        {
            Elem = elem;
            Template = template;
            ImageKey = template.formName;

            roundForSmallDimension = SupportMath.CheckNeedsRoundSmallDimension(elem);

            foreach (ScetchParameter param in Template.parameters)
            {
                string paramName = param.Name;
                string textvalue = param.value;
                ImageKey += "#" + paramName + "~" + textvalue; 
                //заменил = на ~, потому что иначе не подгружаются картинки при экспорте в dwg
            }
        }

        public static string GenerateTemporary(string templateImagePath, List<ScetchParameter> parameters)
        {
            string folder = System.IO.Path.GetDirectoryName(templateImagePath);
            Bitmap templateImage = new Bitmap(templateImagePath);
            WriteBitmap(templateImage, parameters);

            string imageGuid = Guid.NewGuid().ToString();
            string tempImagePath = System.IO.Path.Combine(folder, "temp_" + imageGuid + ".bmp");
            templateImage.Save(tempImagePath);
            return tempImagePath;
        }

        public void Generate(string imagePrefix)
        {
            Debug.WriteLine("Generate new image, prefix " + imagePrefix);
            Bitmap templateImage = new Bitmap(Template.templateImagePath);

            WriteBitmap(templateImage, Template.parameters);

            ScetchImagePath = System.IO.Path.Combine(@"C:\RebarScetch\", imagePrefix + "_" + ImageKey + ".bmp");
            
            templateImage.Save(ScetchImagePath);
            Debug.WriteLine("New bitmap path: " + ScetchImagePath);
        }




        public static void WriteBitmap(Bitmap templateImage, List<ScetchParameter> parameters)
        {
            Debug.WriteLine("Write text to bitmap, parameters count: " + parameters.Count.ToString());
            Graphics gr = Graphics.FromImage(templateImage);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Default;
            //StringFormat format = new StringFormat()
            //{
            //    Alignment = StringAlignment.Center,
            //    LineAlignment = StringAlignment.Center,
            //};
            StringFormat format = StringFormat.GenericTypographic;
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            

            foreach (ScetchParameter param in parameters)
            {
                float b = param.PositionX;
                float h = param.PositionY;
                float angle = param.Rotation;

                float fontSize2 = SupportSettings.fontSize;
                if (param.IsVariable) fontSize2 = fontSize2 * 0.8f;
                Font fnt = new Font(SupportSettings.fontName, fontSize2, SupportSettings.fontStyle);

                if(param.value.EndsWith("°"))
                {
                    fnt = new Font("Isocpeur", fontSize2, SupportSettings.fontStyle);
                }

                gr.TranslateTransform(b, h);
                gr.RotateTransform(-angle);

                float widthScale = 0.85f;

                if (param.NeedsWrap)
                {
                    if (param.value.Length > 10) widthScale = 0.5f;
                    else if (param.value.Length > 6) widthScale = 0.6f;
                    else if (param.value.Length > 3) widthScale = 0.7f;
                }
                gr.ScaleTransform(widthScale, 1f);

                gr.DrawString(param.value, fnt, Brushes.Black, 0, 0, format);

                //string unicodeConvert = Uri.UnescapeDataString(param.value);
                //gr.DrawString(unicodeConvert, fnt, Brushes.Black, 0, 0, format);

                //System.Globalization.CultureInfo _provider = new System.Globalization.CultureInfo("en-us");
                //String drawString = param.value.ToString(_provider);
                //gr.DrawString(drawString, fnt, Brushes.Black, 0, 0, format);

                if (param.HaveSpacing)
                {
                    float heigthSpacingPlacement = 1.8f * fontSize2;
                    gr.DrawString(param.SpacingValue, fnt, Brushes.Black, 0, heigthSpacingPlacement, format);
                }

                gr.ScaleTransform(1 / widthScale, 1f);
                gr.RotateTransform(angle);
                gr.TranslateTransform(-b, -h);
            }
            Debug.WriteLine("Write bitmap success");
        }


        public bool Equals(ScetchImage other)
        {
            if (this.Template.formName != other.Template.formName) return false;
            if (this.Template.parameters.Count != other.Template.parameters.Count) return false;

            for (int i = 0; i < this.Template.parameters.Count; i++)
            {
                string val1 = this.Template.parameters[i].value;
                string val2 = other.Template.parameters[i].value;
                double val1double = 0;
                double val2double = 0;
                bool check1IsDouble = double.TryParse(val1, out val1double);
                bool check2IsDouble = double.TryParse(val2, out val2double);
                if (check1IsDouble && check2IsDouble)
                {
                    val1double = SupportMath.RoundMillimeters(val1double, this.roundForSmallDimension);
                    val2double = SupportMath.RoundMillimeters(val2double, other.roundForSmallDimension);
                    if (val1double != val2double) return false;
                }

                if (val1 != val2) return false;
            }
            return true;
        }



    }
}
