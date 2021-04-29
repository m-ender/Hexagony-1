using System;
using System.Collections.Generic;
using System.Text;

namespace Hexagony
{
    class Grid
    {
        public int Size { get; }
        private readonly Rune[,] _grid;
        private readonly Dictionary<PointAxial, (int x, int y)> _axialToIndex;
        private readonly int[] _lineLengths;

        public Grid(int size)
            : this(size, null)
        { }

        public Grid(Grid other, Rune[] newSource)
        {
            Size = other.Size;
            _grid = new Rune[2 * Size - 1, 2 * Size - 1];
            _axialToIndex = other._axialToIndex;
            _lineLengths = other._lineLengths;

            ReplaceSource(newSource);
        }

        private Grid(int size, IReadOnlyList<Rune> data)
        {
            Size = size;

            _axialToIndex = new();

            for (int r = -size + 1; r <= size - 1; ++r)
                for (int q = -size + 1 - Math.Min(r, 0); q <= size - 1 - Math.Max(r, 0); ++q)
                {
                    PointAxial p = new(q, r);
                    _axialToIndex[p] = AxialToIndex(p);
                }

            _lineLengths = new int[2*size-1];
            _grid = new Rune[2 * size - 1, 2 * size - 1];

            using (var e = data?.GetEnumerator())
                for (int y = 0; y < 2 * size - 1; ++y)
                {
                    _lineLengths[y] = 2 * size - 1 - Math.Abs(size - 1 - y);
                    
                    for (int x = 0; x < _lineLengths[y]; ++x)
                        _grid[y, x] = e != null && e.MoveNext()
                            ? e.Current
                            : new Rune('.');
                }
        }

        public static Grid Parse(string input)
        {
            var index = 0;
            var data = new List<Rune>();
            foreach (var rune in input.EnumerateRunes())
            {
                switch (rune.Value)
                {
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\v':
                    case '\f':
                    case '\r':
                    case '`':
                        // Ignore specific whitespace chars.
                        continue;
                }

                var position = new Position(index, rune.Utf16SequenceLength);
                index += position.Length;
                data.Add(rune);
            }

            var size = 1;
            while (3 * size * (size - 1) + 1 < data.Count)
                size++;
            return new Grid(size, data);
        }

        private void ReplaceSource(Rune[] source)
        {
            int i = 0;
            for (int y = 0; y < _grid.Length; ++y)
                for (int x = 0; x < _lineLengths[y]; ++x)
                {
                    _grid[y,x] = source[i++];
                    if (i >= source.Length)
                        return;
                }
        }

        public Rune this[PointAxial coords]
        {
            get
            {
                var (x, y) = _axialToIndex[coords];
                return _grid[y,x];
            }
        }

        private (int, int) AxialToIndex(PointAxial coords)
        {
            var (q, r) = coords;
            // var y = -x - z;
            //if (Ut.Max(Math.Abs(x), Math.Abs(y), Math.Abs(z)) >= Size)
            //    return null;

            var y = r + Size - 1;
            var x = q + Math.Min(y, Size - 1);
            return (x, y);
        }

        public override string ToString() => "";
        //_grid.Select(line =>
        //    new string(' ', 2 * Size - line.Length) + line.JoinString(" "))
        //.JoinString(Environment.NewLine);

        /// <summary>
        /// Return a string containing the grid and the range of coordinates for each row.
        /// </summary>
        public string ToDebugString() => "";
            //_grid
            //    .Select((line, index) =>
            //    {
            //        var padding = new string(' ', 2 * Size - line.Length);
            //        var row = index - Size + 1;
            //        var q1 = Math.Max(1 - Size, -index);
            //        var q2 = q1 + line.Length - 1;
            //        return padding + line.JoinString(" ") + padding +
            //            $"    Q: [{q1,3},{q2,3}], R: {row,2}";
            //    })
            //    .JoinString(Environment.NewLine);
    }
}
