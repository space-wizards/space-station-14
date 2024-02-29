using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a set of loadouts for a particular slot.
/// </summary>
[Prototype("loadoutGroup")]
public sealed class LoadoutGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// User-friendly name for the group.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// If optional then no loadouts in the group need to be specified.
    /// </summary>
    [DataField]
    public bool Optional;

    [DataField(required: true)]
    public List<ProtoId<LoadoutPrototype>> Loadouts = new();
}
