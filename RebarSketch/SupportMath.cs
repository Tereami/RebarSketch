using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RebarSketch
{
    public static class SupportMath
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">Length in feets</param>
        /// <param name="minValueForRound">No round if value less than this</param>
        /// <param name="lengthAccuracy"></param>
        /// <returns>Rounded value in millimeters</returns>
        public static double RoundMillimeters(double value, double minValueForRound, double lengthAccuracy)
        {
            double mms = value * 304.8;
            mms = Math.Round(mms, 1);
            if (value > minValueForRound)
            {
                mms = lengthAccuracy * Math.Round(mms / lengthAccuracy);
            }
            else
            {
                mms = Math.Round(mms);
            }
            return mms;
        }

        public static double RoundDegrees(double value)
        {
            double degrees = (360 * value) / (2 * Math.PI);
            double rd = Math.Round(degrees, 1);
            return rd;
        }
    }
}
