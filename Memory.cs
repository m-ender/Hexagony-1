using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hexagony
{
    class Memory
    {
        private readonly Dictionary<(PointAxial point, Direction dir), long> _edges = new();
        private PointAxial _mp;
        private Direction _dir = Direction.East;
        private bool _cw;

        public Memory() { }

        public Memory(Memory other)
        {
            _edges = new(other._edges);
            _mp = other._mp;
            _dir = other._dir;
            _cw = other._cw;
        }

        public void Reverse() { _cw = !_cw; }

        public void MoveLeft() => (_mp, _dir, _cw) = LeftIndex;

        public void MoveRight() => (_mp, _dir, _cw) = RightIndex;

        public void Set(long value) => _edges[(_mp, _dir)] = value;

        public long Get() =>
            _edges.TryGetValue((_mp, _dir), out var value) ? value : 0;

        public long GetLeft()
        {
            var index = LeftIndex;
            return _edges.TryGetValue((index.point, index.dir), out var value) ? value : 0;
        }

        public long GetRight()
        {
            var index = RightIndex;
            return _edges.TryGetValue((index.point, index.dir), out var value) ? value : 0;
        }

        private (PointAxial point, Direction dir, bool cw) LeftIndex
        {
            get
            {
                var mp = _mp;
                var dir = _dir;
                var cw = _cw;

                if (dir is NorthEast)
                {
                    mp = cw ? new PointAxial(mp.Q + 1, mp.R - 1) : new PointAxial(mp.Q, mp.R - 1);
                    dir = Direction.SouthEast;
                    cw = !cw;
                }
                else if (dir is East)
                {
                    mp = cw ? new PointAxial(mp.Q, mp.R + 1) : mp;
                    dir = Direction.NorthEast;
                }
                else if (dir is SouthEast)
                {
                    mp = cw ? new PointAxial(mp.Q - 1, mp.R + 1) : mp;
                    dir = Direction.East;
                }

                return (mp, dir, cw);
            }
        }

        private (PointAxial point, Direction dir, bool cw) RightIndex
        {
            get
            {
                var mp = _mp;
                var dir = _dir;
                var cw = _cw;

                if (dir is NorthEast)
                {
                    mp = cw ? mp : new PointAxial(mp.Q, mp.R - 1);
                    dir = Direction.East;
                }
                else if (dir is East)
                {
                    mp = cw ? mp : new PointAxial(mp.Q + 1, mp.R - 1);
                    dir = Direction.SouthEast;
                }
                else if (dir is SouthEast)
                {
                    mp = cw ? new PointAxial(mp.Q - 1, mp.R + 1) : new PointAxial(mp.Q, mp.R + 1);
                    dir = Direction.NorthEast;
                    cw = !cw;
                }

                return (mp, dir, cw);
            }
        }

        public string ToDebugString() =>
            new StringBuilder()
                .AppendLine("Memory (values are stored on the E, NE, and SE edges of the hexagons indicated by the coordinates):")
                .AppendJoin(Environment.NewLine, _edges
                    .Select(x =>
                        (sort: (x.Key.point.Q, x.Key.point.R, -x.Key.dir.Vector.R), text: FormatValue(x.Key.point, x.Key.dir, x.Value)))
                    .OrderBy(x => x.sort)
                    .Select(x => x.text))
                .AppendLine()
                .AppendLine("Pointer:")
                .AppendLine(FormatPosition(_mp, _dir))
                .Append($"Clockwise: {_cw}")
                .ToString();

        private static string FormatPosition(PointAxial p, Direction dir) =>
            $"(Q: {p.Q,3}, R: {p.R,3}, Dir: {dir,2})";

        private string FormatValue(PointAxial p, Direction dir, long value) =>
            $"{FormatPosition(p, dir)}: {value,6}" + (_mp == p && _dir == dir ? " (active)" : null);
    }
}
