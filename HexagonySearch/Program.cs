using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Hexagony;
using RT.Util.ExtensionMethods;

namespace HexagonySearch
{
    class Program
    {
        static void Main(string[] args)
        {
            string targetString = "0\n1\n1\n2\n3\n5\n8\n13\n21\n34\n55\n89\n144\n233\n377\n610\n987\n1597\n2584\n4181\n6765\n10946\n17711\n28657\n46368\n75025\n121393\n196418\n317811\n514229\n832040";

            Rune[] sourceTemplate = @"!)!............".EnumerateRunes().ToArray();
            List<int> emptySlots = new();
            for (int i = 0; i < sourceTemplate.Length; ++i)
                if (sourceTemplate[i] == new Rune('.'))
                    emptySlots.Add(i);

            List<Rune> requiredRunes = @";@".EnumerateRunes().ToList();
            List<Rune> availableRunes = @"\/_|<>{}'""".EnumerateRunes().ToList();

            List<Rune>[] requiredPermutations = GetPermutations(requiredRunes).ToArray();
            List<Rune>[] freeWords = GetWords(availableRunes, emptySlots.Count - requiredRunes.Count).ToArray();

            Stopwatch timer = Stopwatch.StartNew();

            int checkedSubsets = 0;
            BigInteger totalSubsets = 1;
            for (int i = 1; i <= requiredRunes.Count; ++i)
            {
                totalSubsets *= emptySlots.Count - i + 1;
                totalSubsets /= i;
            }

            // Create base instance that runs through fixed prefix
            HexagonyEnv hexagony = new(string.Concat(sourceTemplate), new MemoryStream())
            {
                MaxTicks = 3,
                TargetOutput = targetString,
            };
            hexagony.Run();

            foreach (List<int> requiredSlots in GetSubsets(emptySlots, requiredRunes.Count))
            {
                foreach (int i in emptySlots)
                    sourceTemplate[i] = new Rune('.');

                foreach (int i in requiredSlots)
                    sourceTemplate[i] = new Rune('X');

                if (checkedSubsets > 0)
                {
                    TimeSpan elapsed = timer.Elapsed;
                    float progress = checkedSubsets / (float)totalSubsets;
                    TimeSpan estimatedTotal = elapsed / progress;
                    TimeSpan estimatedRemaining = estimatedTotal - elapsed;

                    Console.WriteLine($"Checking templates: {string.Concat(sourceTemplate)} ... {elapsed.ToString(@"d\.hh\:mm\:ss")} / {estimatedTotal.ToString(@"d\.hh\:mm\:ss")} ({estimatedRemaining.ToString(@"d\.hh\:mm\:ss")} remaining)");
                }
                else
                {
                    Console.WriteLine($"Checking templates: {string.Concat(sourceTemplate)}");
                }
                ++checkedSubsets;

                List<int> freeSlots = emptySlots.Where(i => !requiredSlots.Contains(i)).ToList();

                foreach (List<Rune> permutation in requiredPermutations)
                {
                    foreach ((int i, Rune rune) in requiredSlots.Zip(permutation))
                        sourceTemplate[i] = rune;

                    Parallel.For(0, freeWords.Length, iWord =>
                    {
                        List<Rune> freeRunes = freeWords[iWord];
                        Rune[] sourceArray = (Rune[])sourceTemplate.Clone();
                        foreach ((int i, Rune rune) in freeSlots.Zip(freeRunes))
                            sourceArray[i] = rune;

                        HexagonyEnv testInstance = new(hexagony);
                        testInstance.ReplaceSource(sourceArray);
                        testInstance.MaxTicks = 15;
                        testInstance.Run();

                        if (!testInstance.Success || testInstance.OutputLength < 1)
                            return;

                        testInstance.MaxTicks = 10000;
                        testInstance.Run();

                        if (testInstance.Success && !testInstance.TimedOut)
                            Console.WriteLine($"SOLUTION! {string.Concat(sourceArray)}");
                    });
                }
            }
        }

        private static IEnumerable<List<T>> GetPermutations<T>(List<T> list)
        {
            if (list.Count < 2)
            {
                yield return list;
                yield break;
            }

            for (int i = 0; i < list.Count; ++i)
            {
                List<T> baseList = new() { list[i] };
                List<T> rest = new(list);
                rest.RemoveAt(i);
                foreach (List<T> subPermutation in GetPermutations(rest))
                {
                    yield return baseList.Concat(subPermutation).ToList();
                }
            }
        }

        private static IEnumerable<List<T>> GetSubsets<T>(List<T> list, int size)
        {
            if (list.Count < size)
                yield break;

            if (size == 0)
            {
                yield return new();
                yield break;
            }

            if (list.Count == size)
            {
                yield return list;
                yield break;
            }

            List<T> rest = list.Skip(1).ToList();
            List<T> baseList = new() { list[0] };
            
            foreach (List<T> subset in GetSubsets(rest, size-1))
                yield return baseList.Concat(subset).ToList();

            foreach (List<T> subset in GetSubsets(rest, size))
                yield return subset;
        }

        private static IEnumerable<List<T>> GetWords<T>(List<T> alphabet, int length)
        {
            if (length == 0)
            {
                yield return new();
                yield break;
            }

            foreach (List<T> prefix in GetWords(alphabet, length - 1))
                foreach (T suffix in alphabet)
                    yield return prefix.Concat(suffix).ToList();
        }
    }
}
