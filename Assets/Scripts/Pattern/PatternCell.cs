using System;

namespace EliminateGame.Pattern
{
    [Serializable]
    public class PatternCell
    {
        public BlockColor Color;

        public PatternCell(BlockColor color)
        {
            Color = color;
        }
    }
}
