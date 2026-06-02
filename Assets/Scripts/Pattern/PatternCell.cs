using System;

namespace EliminateGame.Pattern
{
    [Serializable]
    public class PatternCell
    {
        public readonly int OriginalRow;
        public readonly int OriginalColumn;
        public BlockColor Color;

        public PatternCell(BlockColor color, int originalRow, int originalColumn)
        {
            Color = color;
            OriginalRow = originalRow;
            OriginalColumn = originalColumn;
        }

        public PatternCell(BlockColor color)
            : this(color, -1, -1)
        {
        }
    }
}
