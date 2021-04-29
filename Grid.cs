using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace Hexagony
{
    class Grid
    {
        public int Size { get; }
        private readonly Rune[][] _grid;

        public Grid(int size)
            : this(size, null)
        { }

        public Grid(Grid other)
        {
            Size = other.Size;
            _grid = other._grid.Select(x => x.ToArray()).ToArray();
        }

        private Grid(int size, IReadOnlyList<(Rune rune, Position position)> data)
        {
            Size = size;

            // ReSharper disable AccessToDisposedClosure
            using (var e = data?.GetEnumerator())
                _grid = Ut.NewArray(2 * size - 1, j =>
                    Ut.NewArray(2 * size - 1 - Math.Abs(size - 1 - j), _ =>
                        e != null && e.MoveNext() ?
                            e.Current.rune :
                            new Rune('.')));
        }

        public static Grid Parse(string input)
        {
            var index = 0;
            var data = new List<(Rune rune, Position position)>();
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
                data.Add((rune, position));
            }

            var size = 1;
            while (3 * size * (size - 1) + 1 < data.Count)
                size++;
            return new Grid(size, data);
        }

        public void ReplaceSource(Rune[] source)
        {
            int i = 0;
            for (int y = 0; y < _grid.Length; ++y)
                for (int x = 0; x < _grid[y].Length; ++x)
                {
                    _grid[y][x] = source[i++];
                    if (i >= source.Length)
                        return;
                }
        }

        public Rune this[PointAxial coords]
        {
            get
            {
                var tup = AxialToIndex(coords);
                return tup == null ? new Rune('.') : _grid[tup.Item1][tup.Item2];
            }
        }

        private Tuple<int, int> AxialToIndex(PointAxial coords)
        {
            var (x, z) = coords;
            // var y = -x - z;
            //if (Ut.Max(Math.Abs(x), Math.Abs(y), Math.Abs(z)) >= Size)
            //    return null;

            var i = z + Size - 1;
            var j = x + Math.Min(i, Size - 1);
            return Tuple.Create(i, j);
        }

        public override string ToString() =>
            _grid.Select(line =>
                new string(' ', 2 * Size - line.Length) + line.JoinString(" "))
            .JoinString(Environment.NewLine);

        /// <summary>
        /// Return a string containing the grid and the range of coordinates for each row.
        /// </summary>
        public string ToDebugString() =>
            _grid
                .Select((line, index) =>
                {
                    var padding = new string(' ', 2 * Size - line.Length);
                    var row = index - Size + 1;
                    var q1 = Math.Max(1 - Size, -index);
                    var q2 = q1 + line.Length - 1;
                    return padding + line.JoinString(" ") + padding +
                        $"    Q: [{q1,3},{q2,3}], R: {row,2}";
                })
                .JoinString(Environment.NewLine);
    }
}
