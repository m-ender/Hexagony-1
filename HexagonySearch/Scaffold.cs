using System.Linq;

namespace HexagonySearch
{
    public class Scaffold
    {
        private readonly MetaOpcode[] program;
        public int[] CommandIndices { get; init; }

        public MetaOpcode this[int i]
        {
            get => program[i];
        }

        public int Length => program.Length;

        public Scaffold(MetaOpcode[] program, int[] commandIndices)
        {
            this.program = program;
            CommandIndices = commandIndices;
        }

        public override string ToString()
        {
            return string.Join(',', program.Select(o => o.ToString()));
        }
    }
}
