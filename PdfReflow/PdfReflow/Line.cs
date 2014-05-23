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

        public Line()
        {
            Words = new List<Word>();
        }

        public void AddWord(Word w)
        {
            if (Words.Count == 0)
            {
                XMin = w.XMin;
                XMax = w.XMax;
                YMin = w.YMin;
                YMax = w.YMax;
            }
            else
            {
                XMin = Math.Min(XMin, w.XMin);
                XMax = Math.Max(XMax, w.XMax);
                YMin = Math.Min(YMin, w.YMin);
                YMax = Math.Max(YMax, w.YMax);
            }
            Words.Add(w);
        }

        public override string ToString()
        {
            return string.Join(" ",Words.Select(w => w.Text));
        }

        /// <summary>
        /// checks if the given word should be added to this line . 
        /// </summary>
        /// <param name="nextWord">The word to check </param>
        /// <returns>true if the word belongs to this line, ie. if based on its position it is likely to be the next word in line</returns>

        public bool ShouldAdd(Word nextWord)
        {
            if (Words.Count() == 0)
            {
                return true;
            }

            /// Check if in same column:
            /// Compare horizontal space to lineheight (we use lineheight as proxy for fontsize)
            /// Close enough? -> same column -> check same line
            float hSpace = nextWord.XMin - XMax;
            if (hSpace > 0 && hSpace < 0.5 * Height)
            {
                ///  Rougly same height on page? 
                float errorMargin = 0.2f * Height;
                if (nextWord.YMin > YMin - errorMargin && nextWord.YMax < YMax + errorMargin)
                {
                    return true;
                }
                // Be a bit more lenient to deal with text on an angle
                errorMargin =  Math.Min(Height,nextWord.Height);
                if (nextWord.YMin > YMin - errorMargin && nextWord.YMax < YMax + errorMargin)
                {
                    return true;
                }
            }
            //if (hSpace < 0 && hSpace)
            //{
            //    /// This may be a line on an angle
            //    /// Try to find the angle and check if it is 'flat' enough
            //    ///hSpace > -0.5 * Height)
            //}
            return false;

        }


    }
}
