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
        /// <summary>
        ///     Path to the sound to be played when a mob slips.
        /// </summary>
        [ViewVariables]
        private string SlipSound { get; set; } = "/Audio/Effects/slip.ogg";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => SlipSound, "slipSound", "/Audio/Effects/slip.ogg");
        }

        protected override void OnSlip()
        {
            if (!string.IsNullOrEmpty(SlipSound))
            {
                EntitySystem.Get<AudioSystem>()
                    .PlayFromEntity(SlipSound, Owner, AudioHelpers.WithVariation(0.2f));
            }
        }
    }
}
