using BrookNovak.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    public class TextBlockYSelector : IIntervalSelector<BoundingBox, float>
    {
        public float GetStart(BoundingBox item)
        {
            return item.YMin;
        }

        public float GetEnd(BoundingBox item)
        {
            return item.YMax;
        }
    }
}