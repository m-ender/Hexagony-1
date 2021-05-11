using JetBrains.Annotations;

namespace HexagonySearch
{
    public record MetaOpcode
    { }

    public record Exit : MetaOpcode
    {
        public override string ToString()
              => "@";
    }

    public record CommandSlot : MetaOpcode
    {
        public int Index { get; init; }

        public override string ToString() 
            => $"_{Index}";
    }

    public record Jump : MetaOpcode
    {
        public int Target { get; init; }

        public override string ToString()
            => $">{Target}";
    }

    public record Branch : MetaOpcode
    {
        public int TargetIfPositive { get; init; }
        public int TargetIfNotPositive { get; init; }

        public override string ToString()
            => $">?{TargetIfPositive}:{TargetIfNotPositive}";
    }

    // We use these during construction to keep our jumps and
    // branches robust as we move the data around.
    public record ReferenceJump : MetaOpcode
    {
        public MetaOpcode Target { get; }

        public ReferenceJump(MetaOpcode target)
        {
            Target = target;
        }
    }

    public record ReferenceBranch : MetaOpcode
    {
        public MetaOpcode TargetIfPositive { get; init; }
        public MetaOpcode TargetIfNotPositive { get; init; }

        public ReferenceBranch(MetaOpcode targetIfPositive, MetaOpcode targetIfNotPositive)
        {
            TargetIfPositive = targetIfPositive;
            TargetIfNotPositive = targetIfNotPositive;
        }
    }
}
