using System.Linq;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Research.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Research.TechnologyDisk.Components;
using Content.Shared.Research.TechnologyDisk.Systems;
using Content.Shared.Storage.Components;

namespace Content.Server.Research.Disk
{
    public sealed partial class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private ResearchSystem _research = default!;
        [Dependency] private TechnologyDiskSystem _technologyDisk = default!;
        [Dependency] private StorageSystem _storage = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ResearchServerComponent, MagnetPickupAttemptEvent>(OnMagnetPickup);
        }

        private void OnMagnetPickup(EntityUid uid, ResearchServerComponent component, ref MagnetPickupAttemptEvent args)
        {
            if (args.Handled || !this.IsPowered(uid, EntityManager))
                return;

            if (TryComp<TechnologyDiskComponent>(args.Item, out var technologyDisk))
            {
                if (!_technologyDisk.TryInsertDisk((args.Item, technologyDisk), uid))
                    return;
            }
            else if (TryComp<ResearchDiskComponent>(args.Item, out var researchDisk))
            {
                _research.ModifyServerPoints(uid, researchDisk.Points, component);
                QueueDel(args.Item);
            }
            else
            {
                return;
            }

            var xform = Transform(args.Item);
            _storage.PlayPickupAnimation(args.Item, xform.Coordinates, Transform(uid).Coordinates, xform.LocalRotation);
            args.Handled = true;
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ResearchServerComponent>(args.Target, out var server))
                return;

            _research.ModifyServerPoints(args.Target.Value, component.Points, server);
            _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            QueueDel(uid);
            args.Handled = true;
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            component.Points = ProtoMan.EnumeratePrototypes<TechnologyPrototype>()
                .Sum(tech => tech.Cost);
        }
    }
}
