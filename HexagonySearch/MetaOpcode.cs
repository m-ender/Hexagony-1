using JetBrains.Annotations;

namespace HexagonySearch
{
    public class MetaOpcode
    {
        public int Address { get; init; }
    }

    public class Exit : MetaOpcode
    {
        public override string ToString()
              => "@";
    }

    public class CommandSlot : MetaOpcode
    {
        public int Index { get; init; }

        public override string ToString() 
            => $"_{Index}";
    }

    public class Loop : MetaOpcode
    {
        public override string ToString()
            => "loop";
    }

    public class Jump : MetaOpcode
    {
        public MetaOpcode Target { get; set; }

        public Jump(MetaOpcode target)
        {
            Target = target;
        }

        public override string ToString()
            => $">{Target.Address}";
    }

    public class Branch : MetaOpcode
    {
        public MetaOpcode TargetIfPositive { get; set; }
        public MetaOpcode TargetIfNotPositive { get; set; }

        public Branch(MetaOpcode targetIfPositive, MetaOpcode targetIfNotPositive)
        {
            TargetIfPositive = targetIfPositive;
            TargetIfNotPositive = targetIfNotPositive;
        }

        public override string ToString()
            => $">?{TargetIfPositive.Address}:{TargetIfNotPositive.Address}";
    }
}
