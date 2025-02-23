using Robust.Shared.Serialization;
using System.Collections;
using System.Text;

namespace Content.Shared.Genetics;

/// <summary>
/// Genome for an organism.
/// Internally represented as a bit array, shown to players as bases using <see cref="GetBases"/>.
/// Other systems can get data using <see cref="GetBool"/> and <see cref="GetInt"/>.
/// Each bit can either be a boolean or be part of a number, which has its bits stored sequentially.
/// Each species has its own unique genome layout that maps bits to data, which is randomized roundstart.
/// Variable length information such as a list cannot be stored here, so you have to pick a max number of them to support.
/// Prototype ids have special support with <see cref="GenomeLayout"/> and <see cref="GenomePrototype"/>.
/// </summary>
[DataDefinition]
public sealed partial class Genome
{
    /// <summary>
    /// Bits that represent the genes bools and ints.
    /// </summary>
    [ViewVariables]
    public BitArray Bits = new BitArray(0);

    private static char[] Bases = new[] { 'A', 'C', 'G', 'T'};

    /// <summary>
    /// Creates a new genome from all zeroes.
    /// </summary>
    public Genome(int count = 0)
    {
        Bits = new BitArray(count);
    }

    /// <summary>
    /// Copy this genome's bits to another one.
    /// </summary>
    public void CopyTo(Genome other)
    {
        other.Bits = new BitArray(Bits);
    }

    /// <summary>
    /// Get the value of a single bool at an index.
    /// If it is out of bounds false is returned.
    /// </summary>
    /// <param name="index">Bit index to get from</param>
    public bool GetBool(int index)
    {
        return index < Bits.Length && Bits[index];
    }

    /// <summary>
    /// Get the value of an integer with multiple bits.
    /// It should be clamped to a reasonable number afterwards.
    /// </summary>
    /// <param name="index">Starting bit index to get from</param>
    /// <param name="bits">Number of bits to read</param>
    public int GetInt(int index, int bits)
    {
        var value = 0;
        for (int i = 0; i < bits; i++)
        {
            var bit = 1 << i;
            if (GetBool(index + i))
            {
                value |= bit;
            }
        }

        return value;
    }

    /// <summary>
    /// Return a base pair string for a range of bits in the genome.
    /// </summary>
    /// <param name="index">Starting bit index to get from</param>
    /// <param name="bases">Number of bases to include in the string</param>
    public string GetBases(int index, int bases)
    {
        var builder = new StringBuilder(bases);
        for (int i = 0; i < bases; i++)
        {
            // 2 bits makes a base
            var c = Bases[GetInt(index + i * 2, 2)];
            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Sets a boolean value at a bit index to a value.
    /// </summary>
    /// <param name="index">Bit index to set</param>
    public void SetBool(int index, bool value)
    {
        Bits[index] = value;
    }

    /// <summary>
    /// Sets the bits of an integer to a value.
    /// This will truncate the integer to fit inside the possible values.
    /// Also handles writing past the end correctly.
    /// </summary>
    /// <param name="index">Starting bit index to set</param>
    /// <param name="bits">Number of bits to set</param>
    /// <param name="value">Number to set</param>
    public void SetInt(int index, int bits, int value)
    {
        for (int i = 0; i < bits; i++)
        {
            var bit = 1 << i;
            Bits[index + i] = (value & bit) != 0;
        }
    }

    /// <summary>
    /// Flips a bit at an index from a 0 to a 1.
    /// </summary>
    /// <param name="index">Bit index to flip</param>
    public void FlipBit(int index)
    {
        Bits[index] = !Bits[index];
    }

    /// <summary>
    /// Sets the 2 bits at an index to the value of a base.
    /// </summary>
    /// <param name="index">Bit index to set at</param>
    /// <param name="c">Base character to set</param>
    public void SetBase(int index, char c)
    {
        int value = 0;
        switch (c)
        {
            case 'A':
                value = 0;
                break;
            case 'C':
                value = 1;
                break;
            case 'G':
                value = 2;
                break;
            case 'T':
                value = 3;
                break;
        }

        SetInt(index, bits: 2, value: value);
    }
}
