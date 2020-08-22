using Content.Client.Atmos;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class CanSeeGasesComponent : Component
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        public override string Name => "CanSeeGases";

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    if(!_overlayManager.HasOverlay(nameof(GasTileOverlay)))
                        _overlayManager.AddOverlay(new GasTileOverlay());
                    break;

                case PlayerDetachedMsg _:
                    if(!_overlayManager.HasOverlay(nameof(GasTileOverlay)))
                        _overlayManager.RemoveOverlay(nameof(GasTileOverlay));
                    break;
            }
        }
    }
}
