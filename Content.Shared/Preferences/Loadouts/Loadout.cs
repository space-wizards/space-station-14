using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Specifies the selected prototype and custom data for a loadout.
/// </summary>
public sealed class Loadout
{
    public ProtoId<LoadoutPrototype> Prototype;
}
