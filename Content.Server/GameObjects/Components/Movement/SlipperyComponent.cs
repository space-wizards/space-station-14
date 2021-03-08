using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSlipperyComponent))]
    public class SlipperyComponent : SharedSlipperyComponent
    {
        /// <summary>
        ///     Path to the sound to be played when a mob slips.
        /// </summary>
        [ViewVariables]
        [DataField("slipSound")]
        public string SlipSound { get; set; } = "/Audio/Effects/slip.ogg";

        protected override void OnSlip()
        {
            if (!string.IsNullOrEmpty(SlipSound))
            {
                EntitySystem.Get<AudioSystem>()
                    .PlayFromEntity(SlipSound, Owner, AudioHelpers.WithVariation(0.2f));
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SlipperyComponentState(ParalyzeTime, IntersectPercentage, RequiredSlipSpeed, LaunchForwardsMultiplier, Slippery);
        }
    }
}
