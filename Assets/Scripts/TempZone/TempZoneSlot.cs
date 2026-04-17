using System;
using EliminateGame.Pattern;

namespace EliminateGame.TempZone
{
    [Serializable]
    public class TempZoneSlot
    {
        public BlockColor Color;

        // Progress mark for Pattern Case A: 0/3, 1/3, 2/3.
        public int ProgressMark;

        public TempZoneSlot(BlockColor color)
        {
            Color = color;
            ProgressMark = 0;
        }

        public void IncreaseProgressMark(int delta, int max)
        {
            ProgressMark = Math.Clamp(ProgressMark + delta, 0, max);
        }
    }
}
