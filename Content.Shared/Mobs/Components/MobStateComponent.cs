using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components
{
    /// <summary>
    ///     When attached to an <see cref="DamageableComponent"/>,
    ///     this component will handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients
    ///     (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(MobStateSystem), typeof(MobThresholdSystem))]
    public sealed partial class MobStateComponent : Component
    {
        //default mobstate is always the lowest state level
        [ViewVariables] public MobState CurrentState { get; set; } = MobState.Alive;

        [DataField("allowedStates")] public HashSet<MobState> AllowedStates = new()
            {
                MobState.Alive,
                MobState.Critical,
                MobState.Dead
            };
    }

    [Serializable, NetSerializable]
    public sealed class MobStateComponentState : ComponentState
    {
        public readonly MobState CurrentState;
        public readonly HashSet<MobState> AllowedStates;

        public MobStateComponentState(MobState currentState, HashSet<MobState> allowedStates)
        {
            CurrentState = currentState;
            AllowedStates = allowedStates;
        }
    }
}
