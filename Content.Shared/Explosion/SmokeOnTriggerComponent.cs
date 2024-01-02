using Content.Shared.Explosion;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Explosion;

/// <summary>
/// Creates a smoke cloud when triggered, with an optional solution to include in it.
/// No sound is played incase a grenade is stealthy, use <see cref="SoundOnTriggerComponent"/> if you want a sound.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedSmokeOnTriggerSystem))]
public sealed partial class SmokeOnTriggerComponent : Component
{
    /// <summary>
    /// How long the smoke stays for, after it has spread.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Duration = 10;

    /// <summary>
    /// How much the smoke will spread.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public int SpreadAmount;

    /// <summary>
    /// Smoke entity to spawn.
    /// Defaults to smoke but you can use foam if you want.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public ProtoId<EntityPrototype> SmokePrototype = "Smoke";

    /// <summary>
    /// Solution to add to each smoke cloud.
    /// </summary>
    /// <remarks>
    /// When using repeating trigger this essentially gets multiplied so dont do anything crazy like omnizine or lexorin.
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Solution Solution = new();
}
