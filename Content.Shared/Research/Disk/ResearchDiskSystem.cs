using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
        }

        private void OnAfterInteract(Entity<ResearchDiskComponent> ent, ref AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!HasComp<ResearchServerComponent>(args.Target))
                return;

            _research.ModifyServerPoints(args.Target.Value, ent.Comp.Points);
            _popupSystem.PopupClient(Loc.GetString("research-disk-inserted", ("points", ent.Comp.Points)), args.Target.Value, args.User);
            PredictedQueueDel(ent);
            args.Handled = true;
        }

        private void OnMapInit(Entity<ResearchDiskComponent> ent, ref MapInitEvent args)
        {
            if (!ent.Comp.UnlockAllTech)
                return;

            ent.Comp.Points = _prototype.EnumeratePrototypes<TechnologyPrototype>()
                .Sum(tech => tech.Cost);
            Dirty(ent);
        }
    }
}
