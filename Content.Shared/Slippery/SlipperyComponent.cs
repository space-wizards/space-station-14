using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Slippery
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class SlipperyComponent : Component
    {
        public override string Name => "Slippery";

        private float _paralyzeTime = 3f;
        private float _intersectPercentage = 0.3f;
        private float _requiredSlipSpeed = 0.1f;
        private float _launchForwardsMultiplier = 1f;
        private bool _slippery = true;
        private SoundSpecifier _slipSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");

        /// <summary>
        ///     List of entities that are currently colliding with the entity.
        /// </summary>
        public readonly HashSet<EntityUid> Colliding = new();

        /// <summary>
        ///     The list of entities that have been slipped by this component, which shouldn't be slipped again.
        /// </summary>
        public readonly HashSet<EntityUid> Slipped = new();

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
        ///     Percentage of shape intersection for a slip to occur.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("intersectPercentage")]
        public float IntersectPercentage
        {
            get => _intersectPercentage;
            set
            {
                if (MathHelper.CloseToPercent(_intersectPercentage, value)) return;

                _intersectPercentage = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Entities will only be slipped if their speed exceeds this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requiredSlipSpeed")]
        public float RequiredSlipSpeed
        {
            get => _requiredSlipSpeed;
            set
            {
                if (MathHelper.CloseToPercent(_requiredSlipSpeed, value)) return;

                _requiredSlipSpeed = value;
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

        /// <summary>
        ///     Whether or not this component will try to slip entities.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slippery")]
        public bool Slippery
        {
            get => _slippery;
            set
            {
                if (_slippery == value) return;

                _slippery = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SlipperyComponentState(ParalyzeTime, IntersectPercentage, RequiredSlipSpeed, LaunchForwardsMultiplier, Slippery, SlipSound.GetSound(), Slipped.ToArray());
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not SlipperyComponentState state) return;

            _slippery = state.Slippery;
            _intersectPercentage = state.IntersectPercentage;
            _paralyzeTime = state.ParalyzeTime;
            _requiredSlipSpeed = state.RequiredSlipSpeed;
            _launchForwardsMultiplier = state.LaunchForwardsMultiplier;
            _slipSound = new SoundPathSpecifier(state.SlipSound);
            Slipped.Clear();

            foreach (var slipped in state.Slipped)
            {
                Slipped.Add(slipped);
            }
        }
    }

    [Serializable, NetSerializable]
    public class SlipperyComponentState : ComponentState
    {
        public float ParalyzeTime { get; }
        public float IntersectPercentage { get; }
        public float RequiredSlipSpeed { get; }
        public float LaunchForwardsMultiplier { get; }
        public bool Slippery { get; }
        public string SlipSound { get; }
        public readonly EntityUid[] Slipped;

        public SlipperyComponentState(float paralyzeTime, float intersectPercentage, float requiredSlipSpeed, float launchForwardsMultiplier, bool slippery, string slipSound, EntityUid[] slipped)
        {
            ParalyzeTime = paralyzeTime;
            IntersectPercentage = intersectPercentage;
            RequiredSlipSpeed = requiredSlipSpeed;
            LaunchForwardsMultiplier = launchForwardsMultiplier;
            Slippery = slippery;
            SlipSound = slipSound;
            Slipped = slipped;
        }
    }
}
