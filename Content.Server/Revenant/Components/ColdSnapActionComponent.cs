using Content.Server.Abilities;

namespace Content.Server.Revenant.Components;

[RegisterComponent, Access(typeof(AbilitySystem))]
public sealed partial class ColdSnapActionComponent : Component
{
    /// <summary>
    ///     The radius around the user that this ability affects.
    /// </summary>
    [DataField]
    public float ColdSnapRadius = 4f;

    /// <summary>
    ///     Energy removed from each tile in Joules.
    /// </summary>
    [DataField]
    public float EnergyChange = -2000f;
}
