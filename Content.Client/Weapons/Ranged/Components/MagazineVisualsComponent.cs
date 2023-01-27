using Content.Client.Weapons.Ranged.Systems;

namespace Content.Client.Weapons.Ranged.Components;

/// <summary>
/// Visualizer for gun mag presence; can change states based on ammo count or toggle visibility entirely.
/// </summary>
[RegisterComponent, Access(typeof(GunSystem))]
public sealed partial class MagazineVisualsComponent : Component
{
    /// <summary>
    /// What RsiState we use.
    /// </summary>
    [DataField("magState")] public string? MagState;

    /// <summary>
    /// How many steps there are
    /// </summary>
    [DataField("steps")] public int MagSteps;

    /// <summary>
    /// Should we hide when the count is 0
    /// </summary>
    [DataField("zeroVisible")] public bool ZeroVisible;
}

public enum GunVisualLayers : byte
{
    Base,
    BaseUnshaded,
    Mag,
    MagUnshaded,
}
