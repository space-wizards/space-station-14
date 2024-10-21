using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
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
    /// Value to store in genome for each prototype.
    /// </summary>
    [DataField]
    public Dictionary<string, GenomePrototypesLayout> Prototypes = new();

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
    /// Gets a prototype on the genome by name.
    /// If the genome value is invalid it will return null.
    /// </summary>
    public IPrototype? GetPrototype(Genome genome, string typeName, string name, IPrototypeManager proto, IReflectionManager reflection)
    {
        var index = GetInt(genome, name);
        var layout = Prototypes[typeName];

        if (index > layout.Ids.Count)
            return null;

        if (!reflection.TryLooseGetType(typeName, out var type))
            return null;

        var id = layout.Ids[index];
        return proto.Index(type, id);
    }

    /// <summary>
    /// Sets an int on the genome by prototype id.
    /// If the prototype is not listed in the layout nothing is done.
    /// </summary>
    public bool SetPrototype(Genome genome, string name, string typeName, string id)
    {
        var layout = Prototypes[typeName];
        if (!layout.Indices.TryGetValue(id, out var index))
            return false;

        SetInt(genome, name, index);
        return true;
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

    public void SetPrototypesLayout(string typeName, GenomePrototypesLayout layout)
    {
        Prototypes[typeName] = layout;
    }

    /// <summary>
    /// Random picks gene values from a mother and father.
    /// All 3 genomes must use this layout or Bad Things can happen.
    /// </summary>
    public void MixGenes(Genome child, Genome mother, Genome father, IRobustRandom random)
    {
        foreach (var (name, (index, bits)) in Values)
        {
            var parent = random.Prob(0.5f)
                ? mother
                : father;
            var value = parent.GetInt(index, bits);
            child.SetInt(index, bits, value);
        }
    }
}

public sealed partial class GenomePrototypesLayout
{
    /// <summary>
    /// Index into <see cref="Ids"/> to store for each prototype id.
    /// </summary>
    [DataField]
    public Dictionary<string, int> Indices = new();

    /// <summary>
    /// Each prototype id, indexed by the number in <see cref="Indices"/>.
    /// </summary>
    [DataField]
    public List<string> Ids = new();

    /// <summary>
    /// Add a prototype id to the layout.
    /// </summary>
    public void Add(string id)
    {
        var index = Ids.Count;
        Ids.Add(id);
        Indices[id] = index;
    }
}
