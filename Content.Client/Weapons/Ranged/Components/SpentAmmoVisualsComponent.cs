using Content.Client.Weapons.Ranged.Systems;

namespace Content.Client.Weapons.Ranged.Components;

[RegisterComponent, Access(typeof(GunSystem))]
public sealed partial class SpentAmmoVisualsComponent : Component
{
    /// <summary>
    /// Should we do "{_state}-spent" or just "spent"
    /// </summary>
    [DataField]
    public bool Suffix = true;

    [DataField]
    public string State = "base";
}

public enum AmmoVisualLayers : byte
{
    Base,
    Tip,
}
