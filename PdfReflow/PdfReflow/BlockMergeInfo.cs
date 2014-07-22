using BrookNovak.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfReflow
{
    public class BlockMergeInfo
    {
        public BlockMergeInfo(int sourceIdx, int targetIdx, TextBlock source, TextBlock target)
        {
            if (sourceIdx < targetIdx)
            {
                SourceIdx = sourceIdx;
                TargetIdx = targetIdx;
                Source = source;
                Target = target;
            }
            else
            {
                // Swap source and target to maintain document order
                SourceIdx = targetIdx;
                TargetIdx = sourceIdx;
                Source = target;
                Target = source;
            }
        }

        public int SourceIdx;

        public int TargetIdx;

        public TextBlock Source;

        public TextBlock Target;

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

        private TextBlock mergedBlock = null;
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
