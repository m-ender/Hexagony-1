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
        private enum MemoryState
        {
            Unknown,
            Positive,
            NonPositive,
        }

        private struct IPState
        {
            public PointAxial Position { get; set; }
            public Direction Direction { get; set; }
            public MemoryState MemoryState { get; set; }
        }

        private readonly Grid grid;
        private Scaffold? scaffold;
        private readonly List<MetaOpcode> program = new();
        private readonly List<int> commandIndices = new();
        // Maps to a segment of linear code. Each opcode is paired with an
        // "address", an incrementing integer.
        private readonly Dictionary<IPState, List<MetaOpcode>> processedStates = new();
        // Maps each command slot to an index in a depth-first order from the
        // entry point.
        private readonly Dictionary<int, int> commandSlotIndices = new();
        // For each opcode, this contains a set of branches and jumps pointing at it.
        private readonly Dictionary<int, HashSet<int>> inverseJumps = new();
        private int nextAddress = 0;

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
                MemoryState = MemoryState.NonPositive,
            };

            CompileSegment(ip);
            NormalizeLoops();

            scaffold = new(program.ToArray(), commandIndices.ToArray());
            return scaffold;
        }

        private void NormalizeLoops()
        {
            
        }

        private List<MetaOpcode> CompileSegment(IPState initialIP, bool skip = false)
        {
            if (processedStates.ContainsKey(initialIP))
                return processedStates[initialIP];

            List<MetaOpcode> segment = new();
            processedStates[initialIP] = segment;

            Dictionary<IPState, int> visitedIPStates = new();
            IPState ip = initialIP;

            while (true)
            {
                IPState[] newIPs = HandleEdges(ip);

                if (newIPs.Length == 2)
                {
                    segment.Add(CompileBranch(newIPs[0], newIPs[1], skip));
                    return segment;
                }

                ip = newIPs[0];

                if (visitedIPStates.TryGetValue(ip, out int target))
                {
                    int address = nextAddress++;
                    Jump jump = new Jump
                    {
                        Address = address,
                        Target = target
                    };
                    RegisterJump(address, target);
                    segment.Add(jump);
                    return segment;
                }

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
                    case '@':
                        segment.Add(new Exit() { Address = nextAddress++ });
                        return segment;
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
                                MemoryState = MemoryState.Positive,
                            };

                            positiveIP = HandleEdges(positiveIP)[0];

                            IPState nonPositiveIP = new IPState
                            {
                                Position = ip.Position + Direction.NorthEast.Vector(),
                                Direction = Direction.NorthEast,
                                MemoryState = MemoryState.NonPositive,
                            };

                            nonPositiveIP = HandleEdges(nonPositiveIP)[0];

                            switch (ip.MemoryState)
                            {
                            case MemoryState.Unknown:
                                segment.Add(CompileBranch(positiveIP, nonPositiveIP));
                                return segment;
                            case MemoryState.Positive:
                                ip = positiveIP;
                                break;
                            case MemoryState.NonPositive:
                                ip = nonPositiveIP;
                                break;
                            }

                            ip.Position -= ip.Direction.Vector();
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
                                MemoryState = MemoryState.Positive,
                            };

                            positiveIP = HandleEdges(positiveIP)[0];

                            IPState nonPositiveIP = new IPState
                            {
                                Position = ip.Position + Direction.SouthWest.Vector(),
                                Direction = Direction.SouthWest,
                                MemoryState = MemoryState.NonPositive,
                            };

                            nonPositiveIP = HandleEdges(nonPositiveIP)[0];

                            switch (ip.MemoryState)
                            {
                            case MemoryState.Unknown:
                                segment.Add(CompileBranch(positiveIP, nonPositiveIP));
                                return segment;
                            case MemoryState.Positive:
                                ip = positiveIP;
                                break;
                            case MemoryState.NonPositive:
                                ip = nonPositiveIP;
                                break;
                            }

                            ip.Position -= ip.Direction.Vector();
                        }
                        else
                            ip.Direction = ip.Direction.ReflectAtGreaterThan(false);
                        break;
                    default:
                        if (!commandSlotIndices.TryGetValue(opCode, out int index))
                        {
                            index = commandSlotIndices.Count;
                            commandSlotIndices[opCode] = index;
                        }

                        int address = nextAddress++;
                        CommandSlot cmdSlot = new CommandSlot
                        {
                            Address = address,
                            Index = index,
                        };
                        segment.Add(cmdSlot);
                        visitedIPStates[ip] = address;
                        ip.MemoryState = MemoryState.Unknown;
                        break;
                    }
                }

                ip.Position += ip.Direction.Vector();
            }
        }

        private void RegisterJump(int from, int to)
        {
            if (!inverseJumps.TryGetValue(to, out HashSet<int>? sources))
            {
                sources = inverseJumps[to] = new HashSet<int>();
            }

            sources.Add(from);
        }

        private Branch CompileBranch(IPState positive, IPState nonPositive, bool skip = false)
        {
            int address = nextAddress++;

            positive.MemoryState = MemoryState.Positive;
            nonPositive.MemoryState = MemoryState.NonPositive;

            if (!processedStates.TryGetValue(positive, out List<MetaOpcode>? positiveSegment))
            {
                positiveSegment = CompileSegment(positive, skip);
            }

            if (!processedStates.TryGetValue(nonPositive, out List<MetaOpcode>? nonPositiveSegment))
            {
                nonPositiveSegment = CompileSegment(nonPositive, skip);
            }

            Branch branch = new Branch
            {
                Address = address,
                TargetIfPositive = positiveSegment[0].Address,
                TargetIfNotPositive = nonPositiveSegment[0].Address,
            };
            RegisterJump(address, branch.TargetIfPositive);
            RegisterJump(address, branch.TargetIfNotPositive);
            return branch;
        }

        private IPState[] HandleEdges(IPState ip)
        {
            PointAxial pos = ip.Position;
            if (grid.Size == 1)
            {
                ip.Position = new PointAxial(0, 0);
                return new[] { ip };
            }

            (int x, int z) = pos;
            int y = -x - z;

            if (Ut.Max(Math.Abs(x), Math.Abs(y), Math.Abs(z)) < grid.Size)
                return new[] { ip };

            bool xBigger = Math.Abs(x) >= grid.Size;
            bool yBigger = Math.Abs(y) >= grid.Size;
            bool zBigger = Math.Abs(z) >= grid.Size;

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

            if (results.Count == 2)
            {
                switch (ip.MemoryState)
                {
                case MemoryState.Positive:
                    results.RemoveAt(1);
                    break;
                case MemoryState.NonPositive:
                    results.RemoveAt(0);
                    break;
                }
            }

            return results.ToArray();
        }
    }
}
