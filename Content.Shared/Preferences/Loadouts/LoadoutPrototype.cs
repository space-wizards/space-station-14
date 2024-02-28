using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Individual loadout item to be applied.
/// </summary>
[Prototype]
public sealed class LoadoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Equipment;

    /// <summary>
    /// Effects to be applied when the loadout is applied.
    /// These can also return true or false for validation purposes.
    /// </summary>
    [DataField]
    public List<LoadoutEffect> Effects = new();
}
