using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Creates a smoke cloud when triggered, with an optional solution to include in it.
/// No sound is played incase a grenade is stealthy, use <see cref="SoundOnTriggerComponent"/> if you want a sound.
/// </summary>
[RegisterComponent, Access(typeof(SmokeOnTriggerSystem))]
public sealed partial class SmokeOnTriggerComponent : Component
{
    /// <summary>
    /// How long the smoke stays for, after it has spread.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 10;

    /// <summary>
    /// How much the smoke will spread.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int SpreadAmount;

    /// <summary>
    /// Smoke entity to spawn.
    /// Defaults to smoke but you can use foam if you want.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<EntityPrototype> SmokePrototype = "Smoke";

    /// <summary>
    /// Solution to add to each smoke cloud.
    /// </summary>
    /// <remarks>
    /// When using repeating trigger this essentially gets multiplied so dont do anything crazy like omnizine or lexorin.
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Solution Solution = new();
}
