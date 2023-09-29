using Robust.Shared.Utility;

namespace Content.Shared.Genetics;

/// <summary>
/// Maps a <see cref="Genome"/>'s bits to bools and ints.
/// Stores indices for every bool and int that can be retrieved.
/// </summary>
public sealed class GenomeLayout
{
    /// <summary>
    /// Indices and bit lengths of every value stored in the genome.
    /// For <see cref="GetBool"/> bit length must be 1.
    /// </summary>
    [DataField]
    public Dictionary<string, (int, ushort)> Values = new();

    /// <summary>
    /// How many bits this genome has total
    /// </summary>
    [DataField]
    public int TotalBits;

    /// <summary>
    /// Get a bool from the genome by name.
    /// </summary>
    public bool GetBool(Genome genome, string name)
    {
        var (index, bits) = Values[name];
        DebugTools.Assert(bits == 1, "Do not use GetBool for int genome values");

        return genome.GetBool(index);
    }

    /// <summary>
    /// Get an int from the genome by name.
    /// </summary>
    public int GetInt(Genome genome, string name)
    {
        var (index, bits) = Values[name];
        return genome.GetInt(index, bits);
    }

    /// <summary>
    /// Sets a bool value on the genome by name.
    /// </summary>
    public void SetBool(Genome genome, string name, bool value)
    {
        var (index, bits) = Values[name];
        DebugTools.Assert(bits == 1, "Do not use SetBool for int genome values");

        genome.SetBool(index, value);
    }

    /// <summary>
    /// Sets an int on the genome by name.
    /// </summary>
    /// <remarks>
    /// Unused bits are silently ignored.
    /// </remarks>
    public void SetInt(Genome genome, string name, int value)
    {
        var (index, bits) = Values[name];
        genome.SetInt(index, bits: bits, value: value);
    }

    /// <summary>
    /// Add a mapping to the layout.
    /// If length is 1 it will be a bool, int otherwise.
    /// </summary>
    /// <param name="name">Name of the value to add</param>
    /// <param name="bits">Number of bits the value has</param>
    public void Add(string name, ushort bits)
    {
        DebugTools.Assert(bits > 0, "Genome value bit count must be positive");

        var index = TotalBits;
        Values.Add(name, (index, bits));
        TotalBits += bits;
    }
}
