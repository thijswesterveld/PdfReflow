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
                if (Children == null || Children.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return totalLineHeight / Children.Where(c => c is Line).Count();
                }
            }
        }

        /// <summary>
        /// The child blocks contained in this block
        /// Children can be either textblocks or individual Children
        /// </summary>
        public List<BoundingBox> Children;

        public TextBlock()
        {
            Children = new List<BoundingBox>();
            XMax = YMax = float.MinValue;
            XMin = YMin = float.MaxValue;
        }

        public TextBlock(Line l)
        {
            Children = new List<BoundingBox>();
            Children.Add(l);
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
            /// Same column?  -> Check if Xmin close enough
            if (Math.Abs(l.XMin - XMin) < 10)
            {
                // Lower on page and vertically close enough?
                if (l.YMin > YMax && l.YMin - YMax < l.Height)
                {
                    return true;
                }
                // Or (for paragraphs at an angle), check for vertical overlap
                if(l.YMin < YMax && l.YMin > YMin)
                {
                    return true;
                }
            }
            return false;
        }

        
        public bool IsSameColumn(TextBlock b)
        {
            /// Same column?  -> Check if blocks start at (roughly) the same X position 
            if ((Math.Abs(b.XMin - XMin) < 5))
            {
                return true;
            }
            return false;
        }

        public void AddLine(Line l)
        {
            Children.Add(l);
            XMin = Math.Min(XMin, l.XMin);
            XMax = Math.Max(XMax, l.XMax);
            YMin = Math.Min(YMin, l.YMin);
            YMax = Math.Max(YMax, l.YMax);
            totalLineHeight += l.Height;
        }

        public void AddChildBlock(TextBlock textBlock)
        {
            Children.Add(textBlock);
            XMin = Math.Min(XMin, textBlock.XMin);
            XMax = Math.Max(XMax, textBlock.XMax);
            YMin = Math.Min(YMin, textBlock.YMin);
            YMax = Math.Max(YMax, textBlock.YMax);
        }

        public void IdentifyHeaders()
        {
            List<BoundingBox> newChildren = new List<BoundingBox>();
            
            Line previousLine = null;
            TextBlock block = null;

            foreach (BoundingBox b in Children)
            {
                if (b is Line)
                {
                    Line l = b as Line;
                    if (previousLine == null)
                    {
                        block = new TextBlock(l);
                    }
                    else
                    {
                        if (l.Height < 0.95 * block.AvgLineHeight)
                        {
                            // previous line(s) are headers; this is body text
                            block.Type = ElementType.Header;
                            newChildren.Add(block);
                            block = new TextBlock(l);
                            block.Type = ElementType.Paragraph;
                        }
                        else if (l.Height > 1.1 * block.AvgLineHeight)
                        {
                            // Font size difference This is a block
                            newChildren.Add(block);
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
                if(b is TextBlock)
                {
                    previousLine = null;
                    block = null;
                    (b as TextBlock).IdentifyHeaders();
                    newChildren.Add(b);
                }
            }
            if(block != null)
            {
                newChildren.Add(block);
            }
            Children = newChildren;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (BoundingBox childBox in Children)
            {
                if (childBox is TextBlock)
                {
                    TextBlock block = childBox as TextBlock;
                    result.Append(block.ToString());
                }
                if (childBox is Line)
                {
                    Line l = childBox as Line;
                    if (Type == ElementType.Header)
                    {
                        result.Append("#");
                    }
                    result.AppendLine(l.ToString());
                }
            }

            // Add empty line at the end of the textblock, but only if the box contains text (to avoid many empty lines on nested blocks)
            if (Children.Where(c => c is Line).Count() > 0)
            {
                result.AppendLine();
            }
            return result.ToString();
        }

        /// <summary>
        /// Remove hyphenation. Terms that are split over multi-
        /// ple Children in same textblock are combined (e.g., multi-ple -> multiple).
        /// </summary>
        public void Dehyphenate()
        {
            Line previousLine = null;
            foreach (BoundingBox c in Children)
            {
                if (c is Line)
                {
                    Line line  = c as Line;
                    if (previousLine != null)
                    {
                        var lastWordInPreviousLine = previousLine.Words.LastOrDefault();
                        if (lastWordInPreviousLine != null && lastWordInPreviousLine.Text.EndsWith("-"))
                        {
                            var firstWordInLine = line.Words.FirstOrDefault();
                            /// Keep hyphens in special cases
                            /// 1. In combinations with "en": peper- en zoutstelletje
                            /// 2. When next word is capitalised: Amsterdam-Rijnkanaal
                            if (firstWordInLine != null && !String.IsNullOrEmpty(firstWordInLine.Text) && firstWordInLine.Text.ToLower() != "en" && Char.IsLower(firstWordInLine.Text[0]))
                            {
                                lastWordInPreviousLine.Text = lastWordInPreviousLine.Text.Substring(0, lastWordInPreviousLine.Text.Length - 1) + firstWordInLine.Text;
                                line.Words.Remove(firstWordInLine);
                            }
                        }
                    }
                    previousLine = line;
                }

                if (c is TextBlock)
                {
                    TextBlock b = c as TextBlock;
                    previousLine = null;
                    b.Dehyphenate();
                }
            }
        }

        public void OrderChildren()
        {
            List<BoundingBox> orderedChildren = new List<BoundingBox>();
            foreach (BoundingBox child in Children.OrderBy(c => (int)(c.YMin/20)).ThenBy(c => (int)(c.XMin/20)))
            {
                TextBlock block = child as TextBlock;
                if (block != null)
                {
                    block.OrderChildren();
                }
                orderedChildren.Add(child);
            }
            Children = orderedChildren;
        }
    }
}
