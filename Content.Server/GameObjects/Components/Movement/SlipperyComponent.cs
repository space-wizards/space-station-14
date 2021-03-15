using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSlipperyComponent))]
    public class SlipperyComponent : SharedSlipperyComponent
    {
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
