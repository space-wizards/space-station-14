using Content.Shared.Emag.Components;
using Content.Shared.Examine;

namespace Content.Shared.Emag.Systems
{
    /// How to add an emag interaction:
    /// 1. Go to the system for the component you want the interaction with
    /// 2. Subscribe to the GotEmaggedEvent
    /// 3. Check if you're trying to fix or sabotage the object ('args.Fixing')
    /// 4. Have some check for if this actually needs to be emagged or is already emagged (to stop charge waste)
    /// 5. Past the check, add all the effects you desire and HANDLE THE EVENT ARGUMENT so a charge is spent
    public sealed class SharedEmagSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmagComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, EmagComponent component, ExaminedEvent args)
        {
            float timeRemaining = component.RechargeTime - component.Accumulator;
            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.Charges)));
            if (component.Charges == component.MaxCharges)
            {
                args.PushMarkup(Loc.GetString("emag-max-charges"));
                return;
            }
            args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", Math.Round(timeRemaining))));
        }
    }

    /// <summary>
    /// Handle the interact event for both emags and emag-fixer.
    /// </summary>
    public sealed class GotEmaggedEvent : HandledEntityEventArgs
    {
        public readonly EntityUid UserUid;
        public readonly bool Fixing;

        public GotEmaggedEvent(EntityUid userUid, bool shouldFix = false)
        {
            UserUid = userUid;
            Fixing = shouldFix;
        }
    }
}
