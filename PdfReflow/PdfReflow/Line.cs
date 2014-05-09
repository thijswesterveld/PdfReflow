using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfReflow
{
    /// <summary>
    /// A single line of text
    /// </summary>
    public class Line : BoundingBox
    {
        /// <summary>
        /// the words in the block
        /// </summary>
        public List<Word> Words;

        public Line(List<Word> lineWords)
        {
            this.Words = lineWords;
            this.XMin = lineWords.Min(w => w.XMin);
            this.XMax = lineWords.Max(w => w.XMax);
            this.YMin = lineWords.Min(w => w.YMin);
            this.YMax = lineWords.Min(w => w.YMax);
        }

        public override string ToString()
        {
            return string.Join(" ",Words.Select(w => w.ToString()));
        }
    }
}
