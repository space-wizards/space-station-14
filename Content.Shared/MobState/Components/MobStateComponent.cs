using System.Collections;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MobState.Components
{
    /// <summary>
    ///     When attached to an <see cref="DamageableComponent"/>,
    ///     this component will handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients
    ///     (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(SharedMobStateSystem))]
    public sealed class MobStateComponent : Component
    {
        /// <summary>
        ///     States that this <see cref="MobStateComponent"/> mapped to
        ///     the amount of damage at which they are triggered.
        ///     A threshold is reached when the total damage of an entity is equal
        ///     to or higher than the int key, but lower than the next threshold.
        ///     Ordered from lowest to highest.
        /// </summary>
        [DataField("thresholds")] public readonly SortedDictionary<int, MobState> _lowestToHighestStates = new();

        //default mobstate is always the lowest state level
        [ViewVariables] public MobState CurrentState { get; set; } = (MobState) 1;

        /// <summary>
        /// Tickets that determine if we should be in a specific state. Tickets are checked from highest enum to lowest.
        /// If tickets are present in a state, that state is switched to, unless that state is a lower enum value.
        /// </summary>
        public ushort[]
            StateTickets =
                new ushort[Enum.GetValues(typeof(MobState)).Length - 1]; //subtract 1 because invalid is not a state

        [ViewVariables] public FixedPoint2? CurrentThreshold { get; set; }

        public IEnumerable<KeyValuePair<int, MobState>> _highestToLowestStates => _lowestToHighestStates.Reverse();
    }

    [Serializable, NetSerializable]
    public sealed class MobStateComponentState : ComponentState
    {
        public readonly FixedPoint2? CurrentThreshold;
        public readonly ushort[] StateTickets;

        public MobStateComponentState(MobStateComponent comp)
        {
            CurrentThreshold = comp.CurrentThreshold;
            StateTickets = comp.StateTickets;
        }
    }
}
