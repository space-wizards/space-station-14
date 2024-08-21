using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Stores a group of loadout effects in a prototype for re-use.
/// </summary>
[Prototype]
public sealed partial class LoadoutEffectGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField(required: true)]
    public List<LoadoutEffect> Effects = new();
}
