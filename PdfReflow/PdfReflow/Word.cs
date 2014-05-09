using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfReflow
{
    /// <summary>
    /// A single word/token in a text
    /// </summary>
    public class Word : BoundingBox
    {
        /// <summary>
        /// The text of the word or token
        /// </summary>
        public string Text;

        /// <summary>
        /// checks if this word is the next word on the same line as previousWord
        /// </summary>
        /// <param name="previousWord"></param>
        /// <returns>true if this word is the next on the same line, false otherwise</returns>
        public bool IsNextOnSameLine(Word previousWord)
        {
            if(YMin == previousWord.YMin)
            {   
                /// Same height on page. Check if in same column:
                /// Compare horizontal space to lineheight (we use lineheight as proxy for fontsize)
                /// Close enough? -> same column -> on same line
                float hSpace = XMin - previousWord.XMax;
                if (hSpace < 2 * Height)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
