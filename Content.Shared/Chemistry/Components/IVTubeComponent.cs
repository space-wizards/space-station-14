using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Stores what IV bag this mob is transfering with.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class IVTubeComponent : Component
    {
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        /// What bag we're supposed to be attached to.
        /// </summary>
        // TODO: Allow multiple IV bags per mob.
        [ViewVariables] public SharedIVBagComponent? Bag { get; set; }

        /// <summary>
        /// Where the mob is so we can rip the tube out (painfully).
        /// </summary>
        [ViewVariables] public EntityCoordinates? MobPosition { get; set; }

        public const float BreakDistance = 30f;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not IVTubeComponentState state) return;

            var bag = state.Bag.GetValueOrDefault();
            if (!bag.IsValid())
            {
                Bag = null;
                return;
            }

            if (!_entMan.TryGetComponent(bag, out SharedIVBagComponent? bagComponent))
            {
                Logger.Warning($"Unable to set IV tube's bag to {bag}");
                return;
            }

            Bag = bagComponent;
        }

        public override ComponentState GetComponentState()
        {
            return Bag == null ? new IVTubeComponentState(null) : new IVTubeComponentState(Bag.Owner);
        }

        [Serializable, NetSerializable]
        private sealed class IVTubeComponentState : ComponentState
        {
            public EntityUid? Bag { get; }

            public IVTubeComponentState(EntityUid? uid)
            {
                Bag = uid;
            }
        }
    }
}
