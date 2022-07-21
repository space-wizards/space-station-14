using Content.Shared.Sound;
using Content.Shared.StepTrigger;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Slippery
{
    /// <summary>
    /// Causes somebody to slip when they walk over this entity.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="StepTriggerComponent"/>, see that component for some additional properties.
    /// </remarks>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class SlipperyComponent : Component
    {
        private float _paralyzeTime = 3f;
        private float _launchForwardsMultiplier = 1f;
        private SoundSpecifier _slipSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");

        /// <summary>
        ///     Path to the sound to be played when a mob slips.
        /// </summary>
        [ViewVariables]
        [DataField("slipSound")]
        public SoundSpecifier SlipSound
        {
            get => _slipSound;
            set
            {
                if (value == _slipSound)
                    return;

                _slipSound = value;
                Dirty();
            }
        }

        /// <summary>
        ///     How many seconds the mob will be paralyzed for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime
        {
            get => _paralyzeTime;
            set
            {
                if (MathHelper.CloseToPercent(_paralyzeTime, value)) return;

                _paralyzeTime = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("launchForwardsMultiplier")]
        public float LaunchForwardsMultiplier
        {
            get => _launchForwardsMultiplier;
            set
            {
                if (MathHelper.CloseToPercent(_launchForwardsMultiplier, value)) return;

                _launchForwardsMultiplier = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new SlipperyComponentState(ParalyzeTime, LaunchForwardsMultiplier, SlipSound.GetSound());
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SlipperyComponentState state) return;

            _paralyzeTime = state.ParalyzeTime;
            _launchForwardsMultiplier = state.LaunchForwardsMultiplier;
            _slipSound = new SoundPathSpecifier(state.SlipSound);
        }
    }

    [Serializable, NetSerializable]
    public sealed class SlipperyComponentState : ComponentState
    {
        public float ParalyzeTime { get; }
        public float LaunchForwardsMultiplier { get; }
        public string SlipSound { get; }

        public SlipperyComponentState(float paralyzeTime, float launchForwardsMultiplier, string slipSound)
        {
            ParalyzeTime = paralyzeTime;
            LaunchForwardsMultiplier = launchForwardsMultiplier;
            SlipSound = slipSound;
        }
    }
}
