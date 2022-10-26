using Content.Shared.Interaction;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Components;
using Content.Server.Popups;
using Content.Server.Research;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _researchSystem = default!;
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

            //get the TechnologyPrototpye by uid
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            protoMan.TryIndex(component.Technology, out TechnologyPrototype? technology);

            //TODO different kind of points add
            server.SpecialisationPoints["points"] += component.Points;
            if (component.Points > 0) {
                _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, Filter.Entities(args.User));
            } else if (technology != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("research-unlock-disk-inserted", ("name", technology.Name)), args.Target.Value, Filter.Entities(args.User));
            }
            EntityManager.QueueDeleteEntity(uid);

            if (!TryComp<TechnologyDatabaseComponent>(server?.Owner, out var clientDatabase))
                return;

            if (technology != null)
                _researchSystem.UnlockTechnology(server, technology, clientDatabase);
        }
    }
}
