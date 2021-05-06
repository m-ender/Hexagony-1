using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hexagony;
using RT.Util;

namespace HexagonySearch
{
    public class ScaffoldCompiler
    {
        private struct IPState
        {
            public PointAxial Position { get; set; }
            public Direction Direction { get; set; }
        }

        private readonly Grid grid;
        private Scaffold? scaffold;
        private List<MetaOpcode> program = new();
        private List<int> commandIndices = new();
        // Maps to an index into the program.
        private Dictionary<IPState, int> processedStates = new();

        public ScaffoldCompiler(string source)
        {
            // Replace '?' with increasing placeholder runes we can use to identify command
            // slots. This would break for grids big enough to reach 36 cells (where this process
            // results in generating a '$').
            source = string.Concat(source.EnumerateRunes()
                .Select((r, i) => {
                    if (r.Value == '?')
                    {
                        commandIndices.Add(i + 1);
                        return new Rune(i + 1);
                    }
                    else
                        return r;
                }));
            grid = Grid.Parse(source);
        }

        public Scaffold Compile()
        {
            if (scaffold is not null)
                return scaffold;

            IPState ip = new()
            {
                Position = new PointAxial(0, -grid.Size + 1),
                Direction = Direction.East,
            };

            CompileSegment(ip);

            scaffold = new(program.ToArray(), commandIndices.ToArray());
            return scaffold;
        }

        private void CompileSegment(IPState initialIP, bool skip = false)
        {
            IPState ip = initialIP;

            while (true)
            {
                IPState[] newIPs = HandleEdges(ip);

                if (newIPs.Length == 2)
                {
                    CompileBranch(newIPs[0], newIPs[1], skip);
                    return;
                }

                ip = newIPs[0];

                if (processedStates.ContainsKey(ip))
                {
                    program.Add(new Jump
                    {
                        Target = processedStates[ip],
                    });
                    return;
                }

                processedStates[ip] = program.Count;

                if (skip)
                    skip = false;
                else
                {
                    int opCode = grid[ip.Position].Value;
                    switch (opCode)
                    {
                    case 0:
                    case '.':
                        break;
                    case '/':
                        ip.Direction = ip.Direction.ReflectAtSlash();
                        break;
                    case '\\':
                        ip.Direction = ip.Direction.ReflectAtBackslash();
                        break;
                    case '_':
                        ip.Direction = ip.Direction.ReflectAtUnderscore();
                        break;
                    case '|':
                        ip.Direction = ip.Direction.ReflectAtPipe();
                        break;
                    case '$':
                        skip = true;
                        break;
                    case '<':
                        if (ip.Direction == Direction.East)
                        {
                            IPState positiveIP = new IPState
                            {
                                Position = ip.Position + Direction.SouthEast.Vector(),
                                Direction = Direction.SouthEast,
                            };

                            positiveIP = HandleEdges(positiveIP)[0];

                            IPState nonPositiveIP = new IPState
                            {
                                Position = ip.Position + Direction.NorthEast.Vector(),
                                Direction = Direction.NorthEast,
                            };

                            nonPositiveIP = HandleEdges(nonPositiveIP).Last();

                            CompileBranch(positiveIP, nonPositiveIP);
                            return;
                        }
                        else
                            ip.Direction = ip.Direction.ReflectAtLessThan(false);
                        break;
                    case '>':
                        if (ip.Direction == Direction.West)
                        {
                            IPState positiveIP = new IPState
                            {
                                Position = ip.Position + Direction.NorthWest.Vector(),
                                Direction = Direction.NorthWest,
                            };

                            positiveIP = HandleEdges(positiveIP)[0];

                            IPState nonPositiveIP = new IPState
                            {
                                Position = ip.Position + Direction.SouthWest.Vector(),
                                Direction = Direction.SouthWest,
                            };

                            nonPositiveIP = HandleEdges(nonPositiveIP).Last();

                            CompileBranch(positiveIP, nonPositiveIP);
                            return;
                        }
                        else
                            ip.Direction = ip.Direction.ReflectAtGreaterThan(false);
                        break;
                    default:
                        program.Add(new CommandSlot
                        {
                            Index = opCode - 1,
                        });
                        break;
                    }
                }

                ip.Position += ip.Direction.Vector();
            }
        }

        private void CompileBranch(IPState positive, IPState nonPositive, bool skip = false)
        {
            int branchIndex = program.Count;

            // Add placeholder because we don't know the indices yet.
            program.Add(new MetaOpcode());
            if (!processedStates.TryGetValue(positive, out int positiveTarget))
            {
                positiveTarget = program.Count;
                CompileSegment(positive, skip);
            }

            if (!processedStates.TryGetValue(nonPositive, out int nonPositiveTarget))
            {
                nonPositiveTarget = program.Count;
                CompileSegment(nonPositive, skip);
            }

            program[branchIndex] = new Branch
            {
                TargetIfPositive = positiveTarget,
                TargetIfNotPositive = nonPositiveTarget,
            };
        }

        private IPState[] HandleEdges(IPState ip)
        {
            PointAxial pos = ip.Position;
            if (grid.Size == 1)
            {
                ip.Position = new PointAxial(0, 0);
                return new[] { ip };
            }

            var (x, z) = pos;
            var y = -x - z;

            if (Ut.Max(Math.Abs(x), Math.Abs(y), Math.Abs(z)) < grid.Size)
                return new[] { ip };

            var xBigger = Math.Abs(x) >= grid.Size;
            var yBigger = Math.Abs(y) >= grid.Size;
            var zBigger = Math.Abs(z) >= grid.Size;

            // Move the pointer back to the hex near the edge
            pos -= ip.Direction.Vector();

            List<IPState> results = new();

            // If only one coordinate is out of bounds, we reflect along
            // that axis. Otherwise we reflect along both, with the positive
            // branch first. That means they need to be ordered x-y, y-z, or
            // z-x.
            if (xBigger && zBigger)
            {
                ip.Position = new PointAxial(pos.Q + pos.R, -pos.R);
                results.Add(ip);
            }
            if (xBigger)
            {
                ip.Position = new PointAxial(-pos.Q, pos.Q + pos.R);
                results.Add(ip);
            }
            if (yBigger)
            {
                ip.Position = new PointAxial(-pos.R, -pos.Q);
                results.Add(ip);
            }
            if (zBigger && !xBigger)
            {
                ip.Position = new PointAxial(pos.Q + pos.R, -pos.R);
                results.Add(ip);
            }

            return results.ToArray();
        }
    }
}
