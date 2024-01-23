using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
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
        [DataField, ViewVariables]
        public MobState CurrentState { get; set; } = MobState.Alive;

        [DataField]
        public HashSet<MobState> AllowedStates = new()
            {
                MobState.Alive,
                MobState.Critical,
                MobState.Dead
            };

        /// <summary>
        /// The CollisonLayer the entity has while alive.
        /// <remarks>
        /// Stored so it can be returned to it's original value when the entities is alive again.
        /// </remarks>
        /// </summary>
        [DataField]
        public int? AliveCollisionLayer;
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
