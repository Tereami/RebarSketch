using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RebarSketch
{
    public class ScetchParameter
    {
        public string Name = "Арм_А";
        public float PositionX = 200;
        public float PositionY = 100;
        public float Rotation = 0;

        public float FontSize = 55;
        public bool IsNarrow = false;

        public double LengthAccuracy = 5;
        public double MinValueForRound = 20;

        [System.Xml.Serialization.XmlIgnore]
        public string value = "Арм_А";
        [System.Xml.Serialization.XmlIgnore]
        public bool IsVariable;
        [System.Xml.Serialization.XmlIgnore]
        public bool HaveSpacing;
        [System.Xml.Serialization.XmlIgnore]
        public string SpacingValue;
        

        public ScetchParameter()
        {

        }
    }
}
