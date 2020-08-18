using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RebarSketch
{
    public static class SupportMath
    {
        public static double RoundMillimeters(double value, bool roundForSmallDetail)
        {
            double mms = value * 304.8;
            mms = Math.Round(mms, 1);
            if(roundForSmallDetail)
            {
                mms = SupportSettings.lengthAccuracy * Math.Round(mms / SupportSettings.lengthAccuracy);
            }
            else
            {
                if (mms < 50)
                {
                    mms = Math.Round(mms);
                }
            }
            return mms;
        }

        public static double RoundDegrees(double value)
        {
            double degrees = (360 * value) / (2 * Math.PI);
            double rd = Math.Round(degrees, 1);
            return rd;
        }


        public static bool CheckNeedsRoundSmallDimension(Autodesk.Revit.DB.Element elem)
        {
            Autodesk.Revit.DB.Parameter classParam = elem.LookupParameter("Арм.КлассЧисло");
            if (classParam != null && classParam.HasValue)
            {
                double rebarClass = classParam.AsDouble();
                if (rebarClass < 0)
                    return false;
            }
            return true;
        }

    }
}
