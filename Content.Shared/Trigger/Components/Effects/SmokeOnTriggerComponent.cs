using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Creates a smoke cloud when triggered, with an optional solution to include in it.
/// No sound is played incase a grenade is stealthy, use <see cref="EmitSoundOnTriggerComponent"/> if you want a sound.
/// If TargetUser is true the smoke is spawned at their location.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SmokeOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// How long the smoke stays for, after it has spread (in seconds).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How much the smoke will spread.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public int SpreadAmount;

    /// <summary>
    /// Smoke entity to spawn.
    /// Defaults to smoke but you can use foam if you want.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId SmokePrototype = "Smoke";

    /// <summary>
    /// Solution to add to each smoke cloud.
    /// </summary>
    /// <remarks>
    /// When using repeating trigger this essentially gets multiplied so dont do anything crazy like omnizine or lexorin.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public Solution Solution = new();
}
