using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

/// <summary>
///     When attached to an <see cref="DamageableComponent"/>,
///     this component will handle critical and death behaviors for mobs.
///     Additionally, it handles sending effects to clients
///     (such as blur effect for unconsciousness) and managing the health HUD.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(MobStateSystem), typeof(MobThresholdSystem))]
public sealed partial class MobStateComponent : Component
{
    /// <summary>
    /// The current mob state the entity is in.
    /// </summary>
    [DataField]
    public MobState CurrentState = MobState.Alive; //default mobstate is always the lowest state level

    /// <summary>
    /// The last state that was received by the client
    /// </summary>
    [ViewVariables]
    public MobState LastReceivedState = MobState.Alive;

    [DataField]
    public HashSet<MobState> AllowedStates = new()
    {
        MobState.Alive,
        MobState.Critical,
        MobState.Dead
    };
}

[Serializable, NetSerializable]
public sealed class MobStateComponentState : ComponentState
{
    public MobState CurrentState;

    public HashSet<MobState> AllowedStates = new();
}
