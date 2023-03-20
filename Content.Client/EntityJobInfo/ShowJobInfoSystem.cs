using Content.Shared.EntityJobInfo;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Inventory;

namespace Content.Client.EntityJobInfo
{
    public sealed class ShowJobInfoSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        private EntityJobInfoOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowJobInfoComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowJobInfoComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowJobInfoComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowJobInfoComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            _overlay = new(EntityManager, _protoMan, _inventorySystem);
        }

        private void OnInit(EntityUid uid, ShowJobInfoComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.AddOverlay(_overlay);
            }


        }
        private void OnRemove(EntityUid uid, ShowJobInfoComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlayMan.RemoveOverlay(_overlay);
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowJobInfoComponent component, PlayerAttachedEvent args)
        {
            _overlayMan.AddOverlay(_overlay);
        }

        private void OnPlayerDetached(EntityUid uid, ShowJobInfoComponent component, PlayerDetachedEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
