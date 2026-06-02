namespace EliminateGame.Pattern
{
    public readonly struct PatternRemovedCell
    {
        public readonly int Row;
        public readonly int Column;
        public readonly BlockColor Color;

        public PatternRemovedCell(int row, int column, BlockColor color)
        {
            Row = row;
            Column = column;
            Color = color;
        }
    }
}
