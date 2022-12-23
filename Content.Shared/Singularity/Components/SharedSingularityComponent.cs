using Robust.Shared.GameStates;

using Content.Shared.Singularity.EntitySystems;

namespace Content.Shared.Singularity.Components;

/// <summary>
/// A component that makes the associated entity accumulate energy when an associated event horizon consumes things.
/// Energy management is server-side.
/// </summary>
[NetworkedComponent]
public abstract class SharedSingularityComponent : Component
{
    /// <summary>
    /// The current level of the singularity.
    /// Used as a scaling factor for things like visual size, event horizon radius, gravity well radius, radiation output, etc.
    /// If you want to set this use <see cref="SharedSingularitySystem.SetLevel"/>().
    /// </summary>
    [DataField("level")]
    [Access(friends:typeof(SharedSingularitySystem), Other=AccessPermissions.Read, Self=AccessPermissions.Read)]
    public byte Level = 1;

    /// <summary>
    /// The amount of radiation this singularity emits per its level.
    /// Has to be on shared in case someone attaches a RadiationPulseComponent to the singularity.
    /// If you want to set this use <see cref="SharedSingularitySystem.SetRadsPerLevel"/>().
    /// </summary>
    [DataField("radsPerLevel")]
    [Access(friends:typeof(SharedSingularitySystem), Other=AccessPermissions.Read, Self=AccessPermissions.Read)]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RadsPerLevel = 2f;
}
