using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// Lawset data used internally.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class SiliconLawset
{
    /// <summary>
    /// List of laws in this lawset.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public List<SiliconLaw> Laws = new();

    /// <summary>
    /// What entity the lawset considers as a figure of authority.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string ObeysTo = string.Empty;

    /// <summary>
    /// A single line used in logging laws.
    /// </summary>
    public string LoggingString()
    {
        var laws = new List<string>(Laws.Count);
        foreach (var law in Laws)
        {
            laws.Add($"{law.Order}: {Loc.GetString(law.LawString)}");
        }

        return string.Join(" / ", laws);
    }

    /// <summary>
    /// Do a clone of this lawset.
    /// It will have unique laws but their strings are still shared.
    /// </summary>
    public SiliconLawset Clone()
    {
        var laws = new List<SiliconLaw>(Laws.Count);
        foreach (var law in Laws)
        {
            laws.Add(law.ShallowClone());
        }

        return new SiliconLawset()
        {
            Laws = laws,
            ObeysTo = ObeysTo
        };
    }
}

/// <summary>
/// This is a prototype for a <see cref="SiliconLawPrototype"/> list.
/// Cannot be used directly since it is a list of prototype ids rather than List<Siliconlaw>.
/// </summary>
[Prototype]
public sealed partial class SiliconLawsetPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The locstring of the lawset for the guidebook entry, if no name is provided, defaults to the ID
    /// </summary>    
    [DataField]
    public LocId? Name = null;

    /// <summary>
    /// List of law prototype ids in this lawset.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<SiliconLawPrototype>> Laws = new();

    /// <summary>
    /// What entity the lawset considers as a figure of authority.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string ObeysTo = string.Empty;
}
