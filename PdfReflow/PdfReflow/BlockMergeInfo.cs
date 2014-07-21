using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    public class BlockMergeInfo
    {
        public BlockMergeInfo(int minIdx, TextBlock source, TextBlock target)
        {
                BlockIdx = minIdx;
                Source = source;
                Target = target;
        }

        public int BlockIdx;

        public TextBlock Source;

        public TextBlock Target;

        public HashSet<TextBlock> Overlapping;

        public bool CanMerge
        {
            get 
            {
                /// we can only merge if the resulting merge block does not overlap with any other blocks,
                /// i.e., if source and target are the only two blocks in the overlapping set
                return Overlapping.Count() ==2;
            }
        }

        public float Distance
        {
            get
            {
                float hDist = Dist1D(Source.XMin, Target.XMin, Source.XMax, Target.XMax);
                float vDist = Dist1D(Source.YMin, Target.YMin, Source.YMax, Target.YMax);
                // return euclidean distance
                return (float)Math.Sqrt(Math.Pow(hDist, 2) + Math.Pow(vDist, 2));
            }
        }

        public TextBlock mergedBlock = null;
        public TextBlock MergedBlock
        {
            get
            {
                if (mergedBlock == null)
                {
                    mergedBlock = new TextBlock();
                    mergedBlock.AddChildBlock(Source);
                    mergedBlock.AddChildBlock(Target);
                }
                return mergedBlock;
            }
        }  
            
        private float Dist1D(float aMin, float bMin, float aMax, float bMax)
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
                return Dist1D(bMin, aMin, bMax, aMax);
            }
        }
    }
}
