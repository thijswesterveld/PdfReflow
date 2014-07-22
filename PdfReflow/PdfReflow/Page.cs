using BrookNovak.Collections;
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
            IdentifyPageOrientation();

            RegroupSplitWords();

            IdentifyLines();

            IdentifyBlocks();

            FixHyphenation();

            MergeBlocks();

            OrderBlocks();

            IdentifyHeaders();
        }

        private void IdentifyPageOrientation()
        {
            if (words != null)
            {
                Word previousWord = null;
                List<float> verticalOffsets = new List<float>();
                foreach (Word w in words)
                {
                    if (previousWord != null)
                    {
                        verticalOffsets.Add((w.YMin - previousWord.YMin) / Math.Max(w.Height, previousWord.Height));
                    }
                    previousWord = w;
                }
                var increasingOffsets = verticalOffsets.Where(o => o > 0.5);
                var decreasingOffsets = verticalOffsets.Where(o => o < -0.5);
                var sameLevelOffsets = verticalOffsets.Where(o => o <= 0.5 && o >= -0.5);
                if (2 * increasingOffsets.Count() < decreasingOffsets.Count() && sameLevelOffsets.Count() > decreasingOffsets.Count())
                {
                    var incAvg = 0.0;
                    var decAvg = 0.0;
                    if (increasingOffsets.Count() > 0)
                    {
                        incAvg = Math.Abs(increasingOffsets.Average());
                    }
                    if (decreasingOffsets.Count() > 0)
                    {
                        decAvg = Math.Abs(decreasingOffsets.Average());
                    }
                    if (decAvg >= 1.0)
                    {
                        if (decreasingOffsets.Count() > 10)
                        {
                            // Page is probably upside down
                            RotatePage();
                        }
                        else
                        {
                            Console.WriteLine("Not enough lines on page {0}", PageNumber);
                        }
                    }
                }
            }
        }

        private void RotatePage()
        {
            foreach (Word w in words)
            {
                Rotate(w);
            }
        }

        private void Rotate(Word w)
        {
            var oldXMax = w.XMax;
            w.XMax = Width - w.XMin;
            w.XMin = Width - oldXMax;

            var oldYMax = w.YMax;
            w.YMax = Height - w.YMin;
            w.YMin = Height - oldYMax;
        }

        /// <summary>
        /// In some cases, pdftotext splits words.
        /// This method tries to regroup them based on position information
        /// </summary>
        private void RegroupSplitWords()
        {
            if(words == null)
            {
                return;
            }
            List<Word> newWords = new List<Word>();
            Word previousWord = null;
            foreach(Word w in words)
            {
                if(previousWord == null)
                {
                    previousWord = w;
                }
                else
                {
                    if (!String.IsNullOrEmpty(w.Text))
                    {
                        /// if consecutive words are roughly on the same height
                        /// and if word spacing is extremely small in comparison to font-height, 
                        /// we assume this should have been a single word and concatenate the words.
                        float xDiff = w.XMin - previousWord.XMax;
                        int dummy; 
                        if (Math.Abs(w.YMin - previousWord.YMin) < w.Height
                            && (xDiff < 0.02 * w.Height  || (xDiff < 0.1 * w.Height && w.Height == previousWord.Height))
                            && xDiff > -0.5 * w.Height
                            && !int.TryParse(previousWord.Text, out dummy)) // do not combine closely-spaced numbered lists
                        {
                                /// Negative spacing occurs for terms ending in soft hyphen (unicode character 173)
                                /// -> We need to remove the hyphen
                                if (xDiff < 0 && previousWord.Text[previousWord.Text.Length - 1] == 173)
                                {
                                    previousWord.Text = previousWord.Text.Substring(0, previousWord.Text.Length - 1);
                                }
                                previousWord.Text += w.Text;
                                previousWord.XMax = w.XMax; // no need to take max here, second word's XMax is always larger
                                previousWord.YMin = Math.Min(previousWord.YMin, w.YMin);
                                previousWord.YMax = Math.Max(previousWord.YMax, w.YMax);   
                        }
                        else
                        {
                            ///Console.WriteLine(string.Join("\t",previousWord,previousWord.XMin,previousWord.XMax,previousWord.YMin,previousWord.YMax));
                            newWords.Add(previousWord);
                            previousWord = w;
                        }
                    }
                }
            }
            newWords.Add(previousWord);
            words = newWords; 
        }

        private void IdentifyLines()
        {
            if (words != null)
            {
                lines = new List<Line>();
                Line currentLine = new Line();
                foreach (Word w in words)
                {
                    if (currentLine.ShouldAdd(w)) 
                    {
                        currentLine.AddWord(w);
                    }
                    else
                    {
                        lines.Add(currentLine);
                        currentLine = new Line();
                        currentLine.AddWord(w);
                    }
                }
                lines.Add(currentLine);
            }
        }

        private List<TextBlock> xOrderedBlocks
        {
            get
            {
                return new List<TextBlock>(blocks.OrderBy(b => b.XMin).ThenByDescending(b => b.XMax));
            }
        }

        private List<TextBlock> yOrderedBlocks
        {
            get
            {
                return new List<TextBlock>(blocks.OrderBy(b => b.YMin).ThenByDescending(b => b.YMax));
            }
        }

        private void IdentifyBlocks()
        {
            if (lines != null)
            {
                blocks = new List<TextBlock>();
                foreach (Line l in lines)
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
        }

        private void FixHyphenation()
        {
            if (blocks != null)
            {
                foreach (TextBlock block in blocks)
                {
                    block.Dehyphenate();
                }
            }
        }

        private void IdentifyHeaders()
        {
            if (blocks != null)
            {
                foreach (TextBlock b in blocks)
                {
                    b.IdentifyHeaders();
                }
            }
        }

        private void OrderBlocks()
        {
            List<TextBlock> newBlocks = new List<TextBlock>();
            if (blocks != null)
            {
                foreach (TextBlock block in blocks.OrderBy(b => b.YMin).ThenBy(b => b.XMin))
                {
                    block.OrderChildren();
                    newBlocks.Add(block);
                }
            }
            blocks = newBlocks;

        }

               
        private void MergeBlocks()
        {
            HashSet<BlockMergeInfo> mergeInfo = new HashSet<BlockMergeInfo>(ClosestMergablePairs());
            
            while(mergeInfo.Count() > 0)
            {
                var mergePair = mergeInfo.OrderBy(m => m.Distance).ThenBy(m => m.SourceIdx).ThenBy(m => m.TargetIdx).FirstOrDefault();
                
                /// find indexes of blocks to merge                
                int idxSource = blocks.FindIndex(b => b == mergePair.Source);
                int idxTarget = blocks.FindIndex(b => b == mergePair.Target);
          
                /// make sure idx of source is smaller than idx of target
                /// -> first remove block at idxTarget, then at idxSource
                /// insert new block at idxSource
                if(idxSource > idxTarget)
                {
                    int tmp = idxSource;
                    idxSource = idxTarget;
                    idxTarget = tmp;
                }
                blocks.RemoveAt(idxTarget);
                blocks.RemoveAt(idxSource);
                blocks.Insert(idxSource, mergePair.MergedBlock);
                
                mergeInfo = new HashSet<BlockMergeInfo>(ClosestMergablePairs());
            }
        }

       
        /// <summary>
        /// Get candidates for merging with a source block 
        /// </summary>
        /// <param name="sourceBlock">the source block for the merge</param>
        /// <param name="blocksToProcess">select blocks from these candidates</param>
        /// <returns>Candidates with for each candidate a set of blocks that overlap with the resulting blogk</returns>
        private IEnumerable<BlockMergeInfo> ClosestMergablePairs()
        {
            if (blocks != null)
            {
                float minMergableDistance = float.MaxValue;
                for (int i = 0; i < blocks.Count(); ++i)
                {
                    for (int j = i + 1; j < blocks.Count(); ++j)
                    {
                        BlockMergeInfo bmi = new BlockMergeInfo(i, j, blocks[i], blocks[j]);
                        // computing overlap is expensive
                        // only check for overlapping blocks if candidate blocks are close enough
                        if (bmi.Distance <= minMergableDistance)
                        {

                            if (CanMerge(blocks[i], blocks[j]))
                            {
                                minMergableDistance = bmi.Distance;
                                yield return bmi;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if the blocks can be merged, i.e., if no other blocks overlap with the resulting mergedblock
        /// </summary>
        /// <param name="blockA"></param>
        /// <param name="blockB"></param>
        /// <returns></returns>
        private bool CanMerge(TextBlock blockA, TextBlock blockB)
        {
            BoundingBox merged = MergedBoundingBox(blockA, blockB);
            // compute merged bounding box
            //var xOverlap =  blockXTree.FindOverlapping(merged);
            //var yOverlap= blockYTree.FindOverlapping(merged);
            //var overlapAlt = xOverlap.Intersect(yOverlap);

            return !blocks.Exists(b => b!= blockA && b!=blockB && b.Overlaps(merged));        
        }

        private BoundingBox MergedBoundingBox(TextBlock blockA, TextBlock blockB)
        {
            return new BoundingBox()
            {
                XMin = Math.Min(blockB.XMin, blockA.XMin),
                XMax = Math.Max(blockB.XMax, blockA.XMax),
                YMin = Math.Min(blockB.YMin, blockA.YMin),
                YMax = Math.Max(blockB.YMax, blockA.YMax),
            };
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
