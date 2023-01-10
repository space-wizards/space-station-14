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
    public sealed class MobStateComponent : Component
    {
        //default mobstate is always the lowest state level
        [ViewVariables] public MobState CurrentState { get; set; } = MobState.Alive;

        [DataField("allowedMobStates")] public HashSet<MobState> AllowedStates = new()
            {
                MobState.Alive,
                MobState.Critical,
                MobState.Dead
            };

        /// <summary>
        /// Tickets that determine if we should be in a specific state. Tickets are checked from highest enum to lowest.
        /// If tickets are present in a state, that state is switched to, unless that state is a lower enum value.
        /// </summary>
        [ViewVariables] public ushort[] StateTickets =
                new ushort[Enum.GetValues(typeof(MobState)).Length - 1]; //subtract 1 because invalid is not a state
    }

    [Serializable, NetSerializable]
    public sealed class MobStateComponentState : ComponentState
    {
        public readonly HashSet<MobState> AllowedStates;
        public readonly ushort[] StateTickets;

        public MobStateComponentState(HashSet<MobState> allowedStates, ushort[] stateTickets)
        {
            AllowedStates = allowedStates;
            StateTickets = stateTickets;
        }
    }
}
