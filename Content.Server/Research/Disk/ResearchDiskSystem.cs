using Content.Shared.Interaction;
using Content.Server.Research.Components;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Robust.Shared.Player;

namespace Content.Server.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ResearchServerComponent>(args.Target, out var server))
                return;

            _research.ChangePointsOnServer(server.Owner, component.Points, server);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            EntityManager.QueueDeleteEntity(uid);
        }
    }
}
