#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Movement.Components
{
    /// <summary>
    ///     The basic player mover with footsteps and grabbing
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IMobMoverComponent))]
    public class SharedPlayerMobMoverComponent : Component, IMobMoverComponent
    {
        public override string Name => "PlayerMobMover";
        public override uint? NetID => ContentNetIDs.PLAYER_MOB_MOVER;

        private float _stepSoundDistance;
        [DataField("grabRange")]
        private float _grabRange = IMobMoverComponent.GrabRangeDefault;
        [DataField("pushStrength")]
        private float _pushStrength = IMobMoverComponent.PushStrengthDefault;

        [DataField("weightlessStrength")]
        private float _weightlessStrength = IMobMoverComponent.WeightlessStrengthDefault;

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityCoordinates LastPosition { get; set; }

        /// <summary>
        ///     Used to keep track of how far we have moved before playing a step sound
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float StepSoundDistance
        {
            get => _stepSoundDistance;
            set
            {
                if (MathHelper.CloseTo(_stepSoundDistance, value)) return;
                _stepSoundDistance = value;
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

        [ViewVariables(VVAccess.ReadWrite)]
        public float PushStrength
        {
            get => _pushStrength;
            set
            {
                if (MathHelper.CloseTo(_pushStrength, value)) return;
                _pushStrength = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeightlessStrength
        {
            get => _weightlessStrength;
            set
            {
                if (MathHelper.CloseTo(_weightlessStrength, value)) return;
                _weightlessStrength = value;
                Dirty();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (!Owner.HasComponent<IMoverComponent>())
            {
                Owner.EnsureComponentWarn<SharedPlayerInputMoverComponent>();
            }
        }

        public override ComponentState GetComponentState(ICommonSession session)
        {
            return new PlayerMobMoverComponentState(_grabRange, _pushStrength, _weightlessStrength);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not PlayerMobMoverComponentState playerMoverState) return;
            GrabRange = playerMoverState.GrabRange;
            PushStrength = playerMoverState.PushStrength;
        }

        [Serializable, NetSerializable]
        private sealed class PlayerMobMoverComponentState : ComponentState
        {
            public float GrabRange;
            public float PushStrength;
            public float WeightlessStrength;

            public PlayerMobMoverComponentState(float grabRange, float pushStrength, float weightlessStrength) : base(ContentNetIDs.PLAYER_MOB_MOVER)
            {
                GrabRange = grabRange;
                PushStrength = pushStrength;
                WeightlessStrength = weightlessStrength;
            }
        }
    }
}
