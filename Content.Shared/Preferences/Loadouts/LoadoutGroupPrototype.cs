using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a set of loadouts for a particular slot.
/// </summary>
[Prototype]
public sealed class LoadoutGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// If optional then no loadouts in the group need to be specified.
    /// </summary>
    [DataField]
    public bool Optional = true;

    [DataField(required: true)]
    public List<LoadoutPrototype> Loadouts = new();
}
