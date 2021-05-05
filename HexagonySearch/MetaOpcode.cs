namespace HexagonySearch
{
    public record MetaOpcode
    { }

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
}
