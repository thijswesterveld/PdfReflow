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

        private void _MergeBlocks()
        {
            Dictionary<TextBlock, KeyValuePair<TextBlock, float>> mergeCandidates = _GetMergeCandidates();
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
                mergeCandidates = _GetMergeCandidates();
            }
        }


        private float _Distance(TextBlock blockA, TextBlock blockB)
        {
            float hDist = _Dist1D(blockA.XMin, blockB.XMin, blockA.XMax, blockB.XMax);
            float vDist = _Dist1D(blockA.YMin, blockB.YMin, blockA.YMax, blockB.YMax);
            // return euclidean distance
            return (float)Math.Sqrt(Math.Pow(hDist, 2) + Math.Pow(vDist, 2));
        }

        private float _Dist1D(float aMin, float bMin, float aMax, float bMax)
        {
            if (aMin <= bMin)
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
                return _Dist1D(bMin, aMin, bMax, aMax);
            }
        }


        /// <summary>
        /// Find candidate blocks for merging. For each (top-level) block in the page find other blocks on the page that 
        /// can be merged without intersecting or including any other blocks
        /// </summary>
        /// <returns>A dictionary with per mergable block in the page the blocks that can be merged </returns>
        private Dictionary<TextBlock, KeyValuePair<TextBlock, float>> _GetMergeCandidates()
        {
            List<TextBlock> processedBlocks = new List<TextBlock>();
            Dictionary<TextBlock, KeyValuePair<TextBlock, float>> mergePairs = new Dictionary<TextBlock, KeyValuePair<TextBlock, float>>();
            if (blocks != null)
            {
                foreach (TextBlock sourceBlock in blocks)
                {
                    processedBlocks.Add(sourceBlock);
                    Dictionary<TextBlock, float> mergeCandidates = _GetMergeCandidates(sourceBlock, blocks.Where(b => !processedBlocks.Contains(b)));
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
        private Dictionary<TextBlock, float> _GetMergeCandidates(TextBlock sourceBlock, IEnumerable<TextBlock> blocksToProcess)
        {
            Dictionary<TextBlock, float> candidates = new Dictionary<TextBlock, float>();
            foreach (TextBlock candidate in blocksToProcess)
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
                if (overlapping.Count() == 2)
                {
                    candidates.Add(candidate, _Distance(sourceBlock, candidate));
                }
            }
            return candidates;
        }




        private void MergeBlocks()
        {
            HashSet<BlockMergeInfo> mergeInfo = new HashSet<BlockMergeInfo>(ClosestMergablePairs());

            //Dictionary<TextBlock, KeyValuePair<TextBlock, float>> mergeCandidates = _GetMergeCandidates();
            
            while(mergeInfo.Where(m => m.CanMerge).Count() > 0)
            {
                var mergePair = mergeInfo.Where(m => m.CanMerge).OrderBy(m => m.Distance).ThenBy(m => m.BlockIdx).FirstOrDefault();
                
                /// find indexes of blocks to merge
                
                int idxSource = blocks.FindIndex(b => b == mergePair.Source);
                int idxTarget = blocks.FindIndex(b => b == mergePair.Target);

                //// START Compare with previous method
                //var _candidate = mergeCandidates.OrderBy(mc => mc.Value.Value).FirstOrDefault();

                //TextBlock _toMergeA = _candidate.Key;
                //TextBlock _toMergeB = _candidate.Value.Key;
                //int _idxSource = blocks.FindIndex(b => b == _toMergeA);
                //int _idxTarget = blocks.FindIndex(b => b == _toMergeB);

                //bool sameBlocks = ((_idxSource == idxSource && _idxTarget == idxTarget) || (_idxSource == idxTarget && _idxTarget == idxSource));
                //if (!sameBlocks)
                //{
                //    var altPair = mergeInfo.Where(m => (m.Source == _toMergeA && m.Target == _toMergeB) || (m.Source == _toMergeB && m.Target == _toMergeA));
                //    var _altCandidate = mergeCandidates.Where(m => (m.Key == mergePair.Source && m.Value.Key == mergePair.Target) || (m.Key == mergePair.Target && m.Value.Key == mergePair.Source));
                //}
                //// END Compare with previous method
                
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
                //UpdateBlockPairs(mergePair, idxTarget, ref mergeInfo);
              //  mergeCandidates = _GetMergeCandidates();
            }
        }

        private void UpdateBlockPairs(BlockMergeInfo mergedPair, int removedIdx, ref HashSet<BlockMergeInfo> mergeInfo)
        {
            // First remove pairs that contain one of the merged blocks            
            mergeInfo.RemoveWhere(m => m.Source == mergedPair.Source || m.Target == mergedPair.Source || m.Source == mergedPair.Target || m.Target == mergedPair.Target);

            /// Then replace the invidual blocks in the overlapping lists with the merged block 
            /// if at least one of the individual blocks was overlapping with a merge-pair, then the new merged block must overlap as well
            foreach (var mergeCandidate in mergeInfo)
            {
                if (mergeCandidate.BlockIdx > removedIdx)
                {
                    mergeCandidate.BlockIdx--;
                }
                int removeCount = mergeCandidate.Overlapping.RemoveWhere(t => t == mergedPair.Target || t == mergedPair.Source);
                if (removeCount > 0 || mergeCandidate.mergedBlock.Overlaps(mergedPair.mergedBlock))
                {
                    mergeCandidate.Overlapping.Add(mergedPair.mergedBlock);
                }
            }

            // Finally find new candidates to merge with the newly merged block
            for (int i = 0; i < blocks.Count(); ++i)
            {
                if (i != mergedPair.BlockIdx)
                {
                    BlockMergeInfo bmi = new BlockMergeInfo(Math.Min(i, mergedPair.BlockIdx), mergedPair.MergedBlock, blocks[i]);
                    bmi.Overlapping = new HashSet<TextBlock>(GetOverlapping(bmi.Source, bmi.Target));
                }
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
            float minMergableDistance = float.MaxValue;
            for (int i = 0; i < blocks.Count(); ++i)
            {
                for (int j = i + 1; j < blocks.Count(); ++j)
                {
                    BlockMergeInfo bmi = new BlockMergeInfo(i, blocks[i], blocks[j]);
                    if(bmi.Distance <= minMergableDistance)
                    {
                        // computing overlap is expensive
                        // only check for overlapping blocks if candidate blocks are close enough
                        bmi.Overlapping = new HashSet<TextBlock>(GetOverlapping(bmi.Source, bmi.Target));
                        if (bmi.CanMerge)
                        {
                            minMergableDistance = bmi.Distance;
                        }
                        yield return bmi;
                    }
                    
                }
            }
        }

        private IEnumerable<BlockMergeInfo> AllPairs()
        {
            for (int i = 0; i < blocks.Count(); ++i)
            {
                for (int j = i + 1; j < blocks.Count(); ++j)
                {
                    BlockMergeInfo bmi = new BlockMergeInfo(i, blocks[i], blocks[j]);
                    bmi.Overlapping = new HashSet<TextBlock>(GetOverlapping(bmi.Source, bmi.Target));
                    yield return bmi;
                }
            }
        }

        private IEnumerable<TextBlock> GetOverlapping(TextBlock blockA, TextBlock blockB)
        {
            BoundingBox merged = MergedBoundingBox(blockA, blockB);
            // compute merged bounding box

            var overlap = blocks.FindAll(b => b.Overlaps(merged));    
            return overlap;
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
