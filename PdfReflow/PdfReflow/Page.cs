using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    /// <summary>
    /// An individual page from a pdf document
    /// </summary>
    public class Page : BoundingBox
    {
        /// <summary>
        /// Ungrouped words on the page
        /// </summary>
        private List<Word> words = null;

        private List<Line> lines = null;

        private List<TextBlock> blocks = null;

        public Page()
        {
        }

        public void AddWord(Word w)
        {
            if (words == null)
            {
                words = new List<Word>();
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
            words.Add(w);

        }

        /// <summary>
        /// The pageNumber
        /// </summary>
        public int PageNumber;

        public void Reflow()
        {
            IdentifyLines();

            IdentifyBlocks();

            IdentifyHeaders();

            OrderBlocks();
        }

        private void IdentifyLines()
        {
            lines = new List<Line>();
            List<Word> lineWords = new List<Word>();
            Word previousWord = null;
            foreach (Word w in words)
            { 
                if(previousWord == null || w.IsNextOnSameLine(previousWord))
                {
                    lineWords.Add(w);
                }
                else 
                {
                    lines.Add(new Line(lineWords));
                    lineWords = new List<Word>();
                    lineWords.Add(w);
                }
                previousWord = w;
            }
            words = null;
        }

        private void IdentifyBlocks()
        {
            blocks = new List<TextBlock>();
            foreach(Line l in lines)
            {
                TextBlock thisLineBlock = blocks.Where(b => b.IsNextLine(l)).FirstOrDefault();
                if (thisLineBlock == null)
                {
                    blocks.Add(new TextBlock(l));
                }
                else
                {
                    thisLineBlock.AddLine(l);
                }
            }
        }

        private void IdentifyHeaders()
        {
            List<TextBlock> newBlocks = new List<TextBlock>();
            foreach(TextBlock b in blocks)
            {
                newBlocks.AddRange(b.IdentifyHeaders());
            }
            blocks = newBlocks;
        }

        private void OrderBlocks()
        {
            /// TODO: try to identify columns and order blocks

        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (blocks != null)
            {
                foreach (TextBlock b in blocks)
                {
                    result.Append(b.ToString());
                }
            }
            return result.ToString();
        }
    }
}
