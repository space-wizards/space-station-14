using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    public sealed class StaticViewerHudSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private ShaderOverlay _staticOverlay = default!;

        public override void Initialize()
        {
            base.Initialize();

            _staticOverlay = new ShaderOverlay(_prototypeManager.Index<ShaderPrototype>("Grainy").Instance().Duplicate());

            SubscribeLocalEvent<StaticViewerComponent, GotEquippedEvent>(OnCompEquip);
            SubscribeLocalEvent<StaticViewerComponent, GotUnequippedEvent>(OnCompUnequip);
        }

        private void OnCompEquip(EntityUid uid, StaticViewerComponent component, GotEquippedEvent args)
        {
            _overlayMan.AddOverlay(_staticOverlay);
        }

        private void OnCompUnequip(EntityUid uid, StaticViewerComponent component, GotUnequippedEvent args)
        {
            _overlayMan.RemoveOverlay(_staticOverlay);
        }
    }
}
