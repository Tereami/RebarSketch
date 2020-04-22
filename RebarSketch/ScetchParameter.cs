using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RebarSketch
{
    public class ScetchParameter
    {
        public string Name;
        public float PositionX;
        public float PositionY;
        public float Rotation;
        public string value;

        public bool IsVariable;
        public bool HaveSpacing;
        public string SpacingValue;

        public bool NeedsWrap;

        public bool needsRoundSmallDimension;
    }
}
