using Content.Shared.EmagFixer.Components;
using Content.Shared.Examine;

namespace Content.Shared.EmagFixer.Systems
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
            SubscribeLocalEvent<EmagFixerComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, EmagFixerComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.Charges)));
            if (component.Charges == component.MaxCharges)
            {
                args.PushMarkup(Loc.GetString("emag-max-charges"));
                return;
            }
        }
    }

    public sealed class GotEmagFixedEvent : HandledEntityEventArgs
    {
        public readonly EntityUid UserUid;

        public GotEmagFixedEvent(EntityUid userUid)
        {
            UserUid = userUid;
        }
    }
}
