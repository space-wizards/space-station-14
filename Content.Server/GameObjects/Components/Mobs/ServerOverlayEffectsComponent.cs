using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ServerOverlayEffectsComponent : SharedOverlayEffectsComponent
    {
        private ScreenEffects _currentOverlay = ScreenEffects.None;

        public override ComponentState GetComponentState()
        {
            return new OverlayEffectComponentState(_currentOverlay);
        }

        public void ChangeOverlay(ScreenEffects effect)
        {
            if (effect == _currentOverlay)
            {
                return;
            }
            _currentOverlay = effect;
            Dirty();
        }
    }
}
