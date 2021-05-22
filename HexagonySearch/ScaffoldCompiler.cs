using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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

            public static bool operator ==(IPState ip1, IPState ip2)
            {
                return ip1.Position == ip2.Position
                    && ip1.Direction == ip2.Direction
                    && ip1.MemoryState == ip2.MemoryState;
            }

            public static bool operator !=(IPState ip1, IPState ip2)
            {
                return !(ip1 == ip2);
            }
        }

        private readonly Grid grid;
        private Scaffold? scaffold;
        private readonly List<MetaOpcode> program = new();
        private readonly List<int> commandIndices = new();
        // Maps to a segment of linear code. Each opcode is paired with an
        // "address", an incrementing integer.
        private readonly Dictionary<IPState, List<MetaOpcode>> processedStates = new();
        private readonly Dictionary<int, MetaOpcode> addressLookup = new();
        private readonly List<List<MetaOpcode>> segments = new();
        // Maps each command slot to an index in a depth-first order from the
        // entry point.
        private readonly Dictionary<int, int> commandSlotIndices = new();
        // For each opcode, this contains a set of branches and jumps pointing at it.
        private readonly Dictionary<MetaOpcode, HashSet<MetaOpcode>> inverseJumps = new();
        private int nextAddress = 0;

        public ScaffoldCompiler(string source)
        {
            // Replace '?' with increasing placeholder runes we can use to identify command
            // slots.
            source = string.Concat(source.EnumerateRunes()
                .Select((r, i) => {
                    if (r.Value == '?')
                    {
                        commandIndices.Add(i + 128);
                        return new Rune(i + 128);
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
            BuildAddressLookup();
            NormalizeLoops();

            scaffold = new(program.ToArray(), commandIndices.ToArray());
            return scaffold;
        }

        private void BuildAddressLookup()
        {
            foreach (List<MetaOpcode> segment in segments)
                foreach (MetaOpcode opcode in segment)
                    addressLookup[opcode.Address] = opcode;
        }

        private void NormalizeLoops()
        {
            for (int i = 0; i < segments.Count; ++i)
            {
                List<MetaOpcode> segment = segments[i];
                if (segment[^1] is not Jump jump)
                    continue;

                // The way we construct the segments, it shouldn't really be possible
                // to end in a jump that goes outside of the segment, but better safe
                // than sorry.
                if (jump.Target.Address < segment[0].Address || jump.Target.Address > jump.Address)
                    continue;

                // TODO: Start loop as early as possible. E.g. if we had a segment
                // that's 12342 which then jumps back to the 3, we could shorten this
                // to 1234 with a jump back to 2.

                int linearLength = jump.Target.Address - segment[0].Address;
                if (linearLength > 0)
                {
                    List<MetaOpcode> linearSegment = segment.Take(linearLength).ToList();
                    segment.RemoveRange(0, linearLength);
                    linearSegment.Add(new Jump(segment[0])
                    {
                        Address = -1, // ???
                    });
                    segments.Insert(i, linearSegment);
                    ++i;
                }

                segment.RemoveAt(segment.Count-1);
                if (segment.Count > 0)
                {
                    // I'm not sure this can happen but you never know.
                    // Any jumps or branches pointing at the final jump get redirected
                    // to the jump's target instead.
                    foreach (List<MetaOpcode> sourceSegment in segments)
                    {
                        if (sourceSegment[^1] is Branch sourceBranch)
                        {
                            if (sourceBranch.TargetIfPositive == jump)
                                sourceBranch.TargetIfPositive = jump.Target;
                            if (sourceBranch.TargetIfNotPositive == jump)
                                sourceBranch.TargetIfNotPositive = jump.Target;
                        }
                        else if (sourceSegment[^1] is Jump sourceJump)
                        {
                            if (sourceJump.Target == jump)
                                sourceJump.Target = jump.Target;
                        }
                    }

                    // TODO: Remove repetition inside the loop, e.g. 112112112 --> 112

                    // Normalise loop segment to lexicographically smallest rotation.
                    List<CommandSlot> loopSegment = segment.Cast<CommandSlot>().ToList();
                    List<CommandSlot> minimalRotation = loopSegment.ToList();
                    for (int j = 1; j < loopSegment.Count; ++j)
                    {
                        loopSegment.Add(loopSegment[0]);
                        loopSegment.RemoveAt(0);
                        for (int k = 0; k < segment.Count; ++k)
                        {
                            if (loopSegment[k].Index < minimalRotation[k].Index)
                                minimalRotation = loopSegment.ToList();
                            else if (loopSegment[k].Index > minimalRotation[k].Index)
                                break;
                        }
                    }
                    segment = segments[i] = minimalRotation.Cast<MetaOpcode>().ToList();
                }

                segment.Add(new Loop { Address = jump.Address });
            }
        }

        private List<MetaOpcode> CompileSegment(IPState initialIP, bool skip = false)
        {
            if (processedStates.ContainsKey(initialIP))
                return processedStates[initialIP];

            List<MetaOpcode> segment = new();
            processedStates[initialIP] = segment;
            segments.Add(segment);

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

                if (visitedIPStates.TryGetValue(ip, out int targetAddress))
                {
                    MetaOpcode target = addressLookup[targetAddress];
                    int address = nextAddress++;
                    Jump jump = new(target)
                    {
                        Address = address,
                    };
                    RegisterJump(jump, target);
                    segment.Add(jump);
                    return segment;
                }

                visitedIPStates[ip] = nextAddress;

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
                            IPState positiveIP = new()
                            {
                                Position = ip.Position + Direction.SouthEast.Vector(),
                                Direction = Direction.SouthEast,
                                MemoryState = MemoryState.Positive,
                            };

                            positiveIP = HandleEdges(positiveIP)[0];

                            IPState nonPositiveIP = new()
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
                            IPState positiveIP = new()
                            {
                                Position = ip.Position + Direction.NorthWest.Vector(),
                                Direction = Direction.NorthWest,
                                MemoryState = MemoryState.Positive,
                            };

                            positiveIP = HandleEdges(positiveIP)[0];

                            IPState nonPositiveIP = new()
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
                        CommandSlot cmdSlot = new()
                        {
                            Address = address,
                            Index = index,
                        };
                        segment.Add(cmdSlot);
                        ip.MemoryState = MemoryState.Unknown;
                        break;
                    }
                }

                ip.Position += ip.Direction.Vector();
            }
        }

        private void RegisterJump(MetaOpcode from, MetaOpcode to)
        {
            if (!inverseJumps.TryGetValue(to, out HashSet<MetaOpcode>? sources))
            {
                sources = inverseJumps[to] = new HashSet<MetaOpcode>();
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

            Branch branch = new(positiveSegment[0], nonPositiveSegment[0])
            {
                Address = address,
            };
            RegisterJump(branch, branch.TargetIfPositive);
            RegisterJump(branch, branch.TargetIfNotPositive);
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
