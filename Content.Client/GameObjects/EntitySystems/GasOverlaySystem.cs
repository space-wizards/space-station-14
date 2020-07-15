using Content.Client.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasOverlaySystem : SharedGasOverlaySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _overlayManager.AddOverlay(new GasOverlay());

            SubscribeNetworkEvent();
        }
    }
}
