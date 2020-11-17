using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMobStateComponent))]
    [ComponentReference(typeof(IMobStateComponent))]
    public class MobStateComponent : SharedMobStateComponent
    {
        public override void OnRemove()
        {
            // TODO: Might want to add an OnRemove() to IMobState since those are where these components are being used
            base.OnRemove();

            if (Owner.TryGetComponent(out ServerOverlayEffectsComponent overlay))
            {
                overlay.ClearOverlays();
            }
        }
    }
}
