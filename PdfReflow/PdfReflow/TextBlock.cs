using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    /// <summary>
    /// A block with text that belongs together
    /// </summary>
    public class TextBlock : BoundingBox
    {
        public ElementType Type;

        private float totalLineHeight = 0;
        public float AvgLineHeight
        {
            get
            {
                if (Lines == null || Lines.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return totalLineHeight / Lines.Count;
                }
            }
        }

        /// <summary>
        /// The lines of text in the block
        /// </summary>
        public List<Line> Lines;

        public TextBlock(Line l)
        {
            Lines = new List<Line>();
            Lines.Add(l);
            XMin = l.XMin;
            XMax = l.XMax;
            YMin = l.YMin;
            YMax = l.YMax;
            totalLineHeight = Height;
        }

        /// <summary>
        /// Checks if the given line is the next line in this block
        /// </summary>
        /// <param name="l">The line to check</param>
        /// <returns>True if the line belongs to this textblock, false otherwise</returns>
        public bool IsNextLine(Line l)
        {
            // Vertically close enough?
            if (l.YMin - YMax < l.Height)
            {
                /// Same column?  -> Check if we have overlap. 
                /// line starts between block start and end
                ///  B: |------------------|
                ///  L:       |----------
                /// or block starts between line start and end
                ///  B:       |----------
                ///  L: |------------------|
                
                if ((l.XMin >= XMin && l.XMin <= XMax) || (XMin >= l.XMin && XMin <= l.XMax))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddLine(Line l)
        {
            Lines.Add(l);
            XMin = Math.Min(XMin, l.XMin);
            XMax = Math.Max(XMax, l.XMax);
            YMin = Math.Min(YMin, l.YMin);
            YMax = Math.Max(YMax, l.YMax);
            totalLineHeight += l.Height;
        }



        public IEnumerable<TextBlock> IdentifyHeaders()
        {
            Line previousLine = null;
            TextBlock block = null;
            foreach(Line l in Lines)
            {
                if (previousLine == null)
                {
                    block = new TextBlock(l);
                }
                else
                {
                    if (l.Height < 0.9 * block.AvgLineHeight)
                    {
                        // previous line(s) are headers; this is body text
                        block.Type = ElementType.Header;
                        yield return block;
                        block = new TextBlock(l);
                        block.Type = ElementType.Paragraph;
                    }
                    else
                    {
                        block.AddLine(l);
                    }
                }
                previousLine = l;
            }
            yield return block;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (Line l in Lines)
            {
                if (Type == ElementType.Header)
                {
                    result.Append("#");
                }
                result.AppendLine(l.ToString());
            }
            if (Type != ElementType.Header)
            {
                result.AppendLine();
                result.AppendLine();
            }
            return result.ToString();
        }

    }
}
