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
                            && (xDiff < 0.02 * w.Height  || (xDiff < 0.1 * w.Height && Math.Round(w.Height,2) == Math.Round(previousWord.Height,2)))
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
            Dictionary<TextBlock, KeyValuePair<TextBlock, float>> mergeCandidates = GetMergeCandidates();
            while (mergeCandidates.Count() > 0)
            {
                var candidate = mergeCandidates.OrderBy(mc => mc.Value.Value).FirstOrDefault();

                TextBlock toMergeA = candidate.Key;
                TextBlock toMergeB = candidate.Value.Key;

                /// find indexes of blocks to merge
                /// because of the way mergeCandidates are computed, idxA is always smaller than idxB
                /// -> first remove block at idxB, then at idxA
                /// insert new block at idxA

                int idxA = blocks.FindIndex(b => b == toMergeA);
                int idxB = blocks.FindIndex(b => b == toMergeB);
                blocks.RemoveAt(idxB);
                blocks.RemoveAt(idxA);
                TextBlock mergedBlock = new TextBlock();
                mergedBlock.AddChildBlock(candidate.Key);
                mergedBlock.AddChildBlock(candidate.Value.Key);
                blocks.Insert(idxA, mergedBlock);
                int numberOfWerkschriftBlocks = blocks.Count(b => b.ToString().Contains("Werkschrift"));
                mergeCandidates = GetMergeCandidates();
            }
        }

        private float Distance(TextBlock blockA, TextBlock blockB)
        {
            float hDist = Dist1D(blockA.XMin, blockB.XMin,blockA.XMax, blockB.XMax);
            float vDist = Dist1D(blockA.YMin, blockB.YMin,blockA.YMax, blockB.YMax);
            // return euclidean distance
            return (float)Math.Sqrt(Math.Pow(hDist, 2) + Math.Pow(vDist, 2));
        }

        private float Dist1D(float aMin, float bMin, float aMax, float bMax)
        {
            if(aMin <= bMin)
            {
                /// A: |-----
                /// /// B:    |-----
                if (bMin >= aMax)
                {
                    /// A: |-----|
                    /// /// B:       |-----|
                    return bMin - aMax;
                }
                else
                {
                    /// A: |---------|
                    /// /// B:    |-----
                    return 0;
                }
            }
            else 
            {
                return Dist1D(bMin, aMin, bMax, aMax);
            }
        }


        /// <summary>
        /// Find candidate blocks for merging. For each (top-level) block in the page find other blocks on the page that 
        /// can be merged without intersecting or including any other blocks
        /// </summary>
        /// <returns>A dictionary with per mergable block in the page the blocks that can be merged </returns>
        private Dictionary<TextBlock,KeyValuePair<TextBlock,float>> GetMergeCandidates()
        {
            List<TextBlock> processedBlocks = new List<TextBlock>();
            Dictionary<TextBlock, KeyValuePair<TextBlock, float>> mergePairs = new Dictionary<TextBlock, KeyValuePair<TextBlock, float>>();
            if (blocks != null)
            {
                foreach (TextBlock sourceBlock in blocks)
                {
                    processedBlocks.Add(sourceBlock);
                    Dictionary<TextBlock, float> mergeCandidates = GetMergeCandidates(sourceBlock, blocks.Where(b => !processedBlocks.Contains(b)));
                    if (mergeCandidates.Count() > 0)
                    {
                        // add closest candidate as mergePair
                        mergePairs.Add(sourceBlock, mergeCandidates.OrderBy(m => m.Value).First());
                    }
                }
            }
            return mergePairs;
        }

        /// <summary>
        /// Get blocks that can be merged with the specified sourceBlock
        /// </summary>
        /// <param name="sourceBlock">the source block for the merge</param>
        /// <param name="blocksToProcess">select mergable blocks from these candidates</param>
        /// <returns>Candidates that can be safely merged without intersecting other blocks on the page</returns>
        private Dictionary<TextBlock,float> GetMergeCandidates(TextBlock sourceBlock, IEnumerable<TextBlock> blocksToProcess)
        {
            Dictionary<TextBlock,float> candidates = new Dictionary<TextBlock,float>();
            foreach(TextBlock candidate in blocksToProcess)
            {
                // compute merged bounding box
                float xMin = Math.Min(candidate.XMin, sourceBlock.XMin);
                float xMax = Math.Max(candidate.XMax, sourceBlock.XMax);
                float yMin = Math.Min(candidate.YMin, sourceBlock.YMin);
                float yMax = Math.Max(candidate.YMax, sourceBlock.YMax);

                /// find blocks on page that overlap with merged bounding box 
                /// For both x and y coördinates, overlapping blocks have either min or max value (or both) in the merged bounding box range
                var horizontalOverlap = blocks.FindAll(b => (b.XMax >= xMin && b.XMax <= xMax) || (b.XMin >= xMin && b.XMin <= xMax));
                var verticalOverlap = blocks.FindAll(b => (b.YMax >= yMin && b.YMax <= yMax) || (b.YMin >= yMin && b.YMin <= yMax));
                var overlapping = horizontalOverlap.Intersect(verticalOverlap);
                
                // If source and candidate are the only blocks in the merged bounding box, the candidate should be kept
                if(overlapping.Count() ==2)
                {
                    candidates.Add(candidate,Distance(sourceBlock,candidate));
                }
            }
            return candidates;
        }

        /// <summary>
        /// Find textblocks that can be merged vertically with the following block without intersecting other blocks
        /// </summary>
        /// <param name="candidateBlocks">The textBlocks to consider</param>
        /// <returns>a dictionary of textblocks grouped by vertical space between block 
        /// and next block at same horizontal position ordered by increasing sapce</returns>
        private SortedDictionary<float, List<TextBlock>> GetVerticalMergeCandidates(List<TextBlock> candidateBlocks)
        {
            // find groups of horizontally aligned blocks
            var xGroups = candidateBlocks.GroupBy(b => b.XMin);

            // build a dictionary of textblocks grouped by vertical space between block and next block at same xPosition
            SortedDictionary<float, List<TextBlock>> vMergeCandidates = new SortedDictionary<float, List<TextBlock>>();

            /// compute vertical space between horizontally alligned blocks
            foreach (var xG in xGroups)
            {
                TextBlock previousBlock = null;
                foreach (TextBlock b in xG.OrderBy(g => g.YMin))
                {
                    if (previousBlock != null)
                    {
                        float verticalSpace = b.YMin - previousBlock.YMax;
                        List<TextBlock> verticalSpaceBlocks;
                        if (!vMergeCandidates.TryGetValue(verticalSpace, out verticalSpaceBlocks))
                        {
                            verticalSpaceBlocks = new List<TextBlock>();
                            vMergeCandidates.Add(verticalSpace, verticalSpaceBlocks);
                        }
                        verticalSpaceBlocks.Add(previousBlock);
                    }
                    previousBlock = b;
                }
            }
            return vMergeCandidates;
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
