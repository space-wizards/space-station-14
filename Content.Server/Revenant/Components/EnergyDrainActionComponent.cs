using Content.Server.Abilities;

namespace Content.Server.Revenant.Components;

[RegisterComponent, Access(typeof(AbilitySystem))]
public sealed partial class EnergyDrainActionComponent : Component
{
    /// <summary>
    ///     The radius around the user that this ability affects
    /// </summary>
    [DataField]
    public float DrainRadius = 4f;

    /// <summary>
    ///     What fraction of the battery's current charge to drain
    /// </summary>
    [DataField]
    public float DrainFraction = 0.3f;
}
