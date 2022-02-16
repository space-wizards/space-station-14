using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Examine;

namespace Content.Shared.Emag.Systems
{
    /// How to add an emag interaction:
    /// 1. Go to the system for the component you want the interaction with
    /// 2. Subscribe to the GotEmaggedEvent
    /// 3. Have some check for if this actually needs to be emagged or is already emagged (to stop charge waste)
    /// 4. Past the check, add all the effects you desire and HANDLE THE EVENT ARGUMENT so a charge is spent
    public sealed class SharedEmagSystem : EntitySystem
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
            if (!args.CanReach || args.Target == null)
                return;

            if (component.Charges <= 0)
                return;

            var emaggedEvent = new GotEmaggedEvent(args.Target.Value);
            RaiseLocalEvent(args.Target.Value, emaggedEvent, false);
            if (emaggedEvent.Handled)
            {
                component.Charges--;
                return;
            }
        }
    }

    public sealed class GotEmaggedEvent : HandledEntityEventArgs
    {
        public readonly EntityUid TargetUid;

        public GotEmaggedEvent(EntityUid targetUid)
        {
            targetUid = TargetUid;
        }
    }
}
