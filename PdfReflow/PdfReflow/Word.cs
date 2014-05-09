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

       
        public override string ToString()
        {
            return Text;
        }
    }
}
