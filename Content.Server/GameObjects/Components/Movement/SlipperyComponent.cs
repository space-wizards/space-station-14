using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSlipperyComponent))]
    public class SlipperyComponent : SharedSlipperyComponent
    {
        private float _paralyzeTime = 2f;
        private float _intersectPercentage = 0.3f;
        private float _requiredSlipSpeed = 0f;
        private bool _slippery;
        private float _launchForwardsMultiplier = 1f;

        /// <summary>
        ///     Path to the sound to be played when a mob slips.
        /// </summary>
        [ViewVariables]
        private string SlipSound { get; set; } = "/Audio/Effects/slip.ogg";

        /// <summary>
        ///     How many seconds the mob will be paralyzed for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override float ParalyzeTime
        {
            get => _paralyzeTime;
            set
            {
                _paralyzeTime = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Percentage of shape intersection for a slip to occur.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override float IntersectPercentage
        {
            get => _intersectPercentage;
            set
            {
                _intersectPercentage = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Entities will only be slipped if their speed exceeds this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override float RequiredSlipSpeed
        {
            get => _requiredSlipSpeed;
            set
            {
                _requiredSlipSpeed = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Whether or not this component will try to slip entities.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public override bool Slippery
        {
            get => _slippery;
            set
            {
                _slippery = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public override float LaunchForwardsMultiplier
        {
            get => _launchForwardsMultiplier;
            set => _launchForwardsMultiplier = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.SlipSound, "slipSound", "/Audio/Effects/slip.ogg");
        }

        protected override void OnSlip()
        {
            if (!string.IsNullOrEmpty(SlipSound))
            {
                EntitySystem.Get<AudioSystem>()
                    .PlayFromEntity(SlipSound, Owner, AudioHelpers.WithVariation(0.2f));
            }
        }

        public override ComponentState GetComponentState()
        {
            return new SlipperyComponentState(_paralyzeTime, _intersectPercentage, _requiredSlipSpeed, _launchForwardsMultiplier, _slippery);
        }
    }
}
