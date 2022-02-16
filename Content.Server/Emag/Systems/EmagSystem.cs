using Content.Server.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Examine;

namespace Content.Server.Emag.Systems
{
    public sealed class EmagSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmagComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<EmagComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, EmagComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.Charges)));
        }

        private void OnAfterInteract(EntityUid uid, EmagComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (component.Charges <= 0)
                return;

            if (HasComp<EmaggableComponent>(args.Target))
            {
                var emaggedEvent = new GotEmaggedEvent(args.Target.Value);
                RaiseLocalEvent(args.Target.Value, emaggedEvent, false);
                component.Charges--;
                return;
            }
        }
    }

    public sealed class GotEmaggedEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid TargetUid;

        public GotEmaggedEvent(EntityUid targetUid)
        {
            targetUid = TargetUid;
        }
    }
}
