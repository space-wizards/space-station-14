using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Components
{
    /// <summary>
    ///     The basic player mover with footsteps and grabbing
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IMobMoverComponent))]
    [NetworkedComponent()]
    public sealed class SharedPlayerMobMoverComponent : Component, IMobMoverComponent
    {
        private float _stepSoundDistance;
        [DataField("grabRange")]
        private float _grabRange = IMobMoverComponent.GrabRangeDefault;
        [DataField("pushStrength")]
        private float _pushStrength = IMobMoverComponent.PushStrengthDefault;

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
                if (MathHelper.CloseToPercent(_stepSoundDistance, value)) return;
                _stepSoundDistance = value;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float GrabRange
        {
            get => _grabRange;
            set
            {
                if (MathHelper.CloseToPercent(_grabRange, value)) return;
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
                if (MathHelper.CloseToPercent(_pushStrength, value)) return;
                _pushStrength = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new PlayerMobMoverComponentState(_grabRange, _pushStrength);
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

            public PlayerMobMoverComponentState(float grabRange, float pushStrength)
            {
                GrabRange = grabRange;
                PushStrength = pushStrength;
            }
        }
    }
}
