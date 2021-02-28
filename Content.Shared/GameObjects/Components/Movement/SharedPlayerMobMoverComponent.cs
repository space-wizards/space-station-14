#nullable enable
using System;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    /// <summary>
    ///     The basic player mover with footsteps and grabbing
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IMobMoverComponent))]
    public class SharedPlayerMobMoverComponent : Component, IMobMoverComponent, ICollideSpecial
    {
        public override string Name => "PlayerMobMover";
        public override uint? NetID => ContentNetIDs.PLAYER_MOB_MOVER;

        private float _stepSoundDistance;
        private float _grabRange;

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityCoordinates LastPosition { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float StepSoundDistance
        {
            get => _stepSoundDistance;
            set
            {
                if (MathHelper.CloseTo(_stepSoundDistance, value)) return;
                _stepSoundDistance = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float GrabRange
        {
            get => _grabRange;
            set
            {
                if (MathHelper.CloseTo(_grabRange, value)) return;
                _grabRange = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.HasComponent<IMoverComponent>())
            {
                Owner.EnsureComponentWarn<SharedPlayerInputMoverComponent>();
            }
        }

        public override ComponentState GetComponentState(ICommonSession session)
        {
            return new PlayerMobMoverComponentState(StepSoundDistance, GrabRange);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not PlayerMobMoverComponentState playerMoverState) return;
            StepSoundDistance = playerMoverState.StepSoundDistance;
            GrabRange = playerMoverState.GrabRange;
        }

        bool ICollideSpecial.PreventCollide(IPhysBody collidedWith)
        {
            // Don't collide with other mobs
            // unless they have combat mode on
            return collidedWith.Entity.HasComponent<IBody>();  /* &&
                (!Owner.TryGetComponent(out SharedCombatModeComponent? ownerCombat) || !ownerCombat.IsInCombatMode) &&
                (!collidedWith.Entity.TryGetComponent(out SharedCombatModeComponent? otherCombat) || !otherCombat.IsInCombatMode);
                */
        }

        [Serializable, NetSerializable]
        private sealed class PlayerMobMoverComponentState : ComponentState
        {
            public float StepSoundDistance;
            public float GrabRange;

            public PlayerMobMoverComponentState(float stepSoundDistance, float grabRange) : base(ContentNetIDs.PLAYER_MOB_MOVER)
            {
                StepSoundDistance = stepSoundDistance;
                GrabRange = grabRange;
            }
        }
    }
}
