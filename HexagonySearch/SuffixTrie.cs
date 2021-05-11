using System.Collections.Generic;

namespace HexagonySearch
{
    // Not to be confused with a suffix tree.
    // The suffixes are read from leaf to root (like the prefixes in
    // a trie) instead of root to leaf as they are in a suffix tree.
    public class SuffixTrie
    {
        public List<MetaOpcode> Suffix { get; init; } = new();
        public List<SuffixTrie> Children { get; init; } = new();
    }
}
