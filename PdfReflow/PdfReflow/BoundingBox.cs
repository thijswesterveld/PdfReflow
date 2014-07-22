using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    /// <summary>
    ///  Element in a document (e.g. word, line, paragraph, etc) with its position
    /// </summary>
    public  class BoundingBox
    {
        /// <summary>
        /// Horizontal leftmost position of the elements bounding box on the page (in pts from top left corner)
        /// </summary>
        public float XMin;

        /// <summary>
        /// Horizontal rightmost position of the elements bounding box on the page (in pts from top left corner)
        /// </summary>
        public float XMax;

        /// <summary>
        /// Vertical topmost position of the elements bounding box on the page (in pts from top left corner)
        /// </summary>
        public float YMin;

        /// <summary>
        /// Vertical bottommost position of the elements bounding box on the page (in pts from top left corner)
        /// </summary>
        public float YMax;

        public float Height
        {
            get { return YMax - YMin; }
        }

        public float Width
        {
            get { return XMax - XMin; }
        }

        public bool Overlaps(BoundingBox other)
        {
            return 
                   this.XMin <= other.XMax   && this.XMax >= other.XMin &&
                   this.YMin <=other.YMax && this.YMax >= other.YMin;
        }
    }
}
