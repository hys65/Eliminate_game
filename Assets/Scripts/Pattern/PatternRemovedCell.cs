namespace EliminateGame.Pattern
{
    public readonly struct PatternRemovedCell
    {
        public readonly int OriginalRow;
        public readonly int OriginalColumn;
        public readonly int CurrentRow;
        public readonly int CurrentColumn;
        public readonly BlockColor Color;

        public int Row => CurrentRow;
        public int Column => CurrentColumn;

        public PatternRemovedCell(
            int originalRow,
            int originalColumn,
            int currentRow,
            int currentColumn,
            BlockColor color)
        {
            OriginalRow = originalRow;
            OriginalColumn = originalColumn;
            CurrentRow = currentRow;
            CurrentColumn = currentColumn;
            Color = color;
        }

        public PatternRemovedCell(int row, int column, BlockColor color)
            : this(row, column, row, column, color)
        {
        }
    }
}
