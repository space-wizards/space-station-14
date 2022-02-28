using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.Player;

namespace Content.Shared.Emag.Systems
{
    /// How to add an emag interaction:
    /// 1. Go to the system for the component you want the interaction with
    /// 2. Subscribe to the GotEmaggedEvent
    /// 3. Have some check for if this actually needs to be emagged or is already emagged (to stop charge waste)
    /// 4. Past the check, add all the effects you desire and HANDLE THE EVENT ARGUMENT so a charge is spent
    public sealed class SharedEmagSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedAdminLogSystem _adminLog = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmagComponent, AfterInteractEvent>(OnAfterInteract);
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

        private void OnAfterInteract(EntityUid uid, EmagComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (component.Charges <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-no-charges"), args.User, Filter.Entities(args.User));
                return;
            }

            var emaggedEvent = new GotEmaggedEvent(args.User);
            RaiseLocalEvent(args.Target.Value, emaggedEvent, false);
            if (emaggedEvent.Handled)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-success",("target", args.Target)), args.User, Filter.Entities(args.User));
                _adminLog.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(args.User):player} emagged {ToPrettyString(args.Target.Value):target}");
                component.Charges--;
                return;
            }
        }
    }

    public sealed class GotEmaggedEvent : HandledEntityEventArgs
    {
        public readonly EntityUid UserUid;

        public GotEmaggedEvent(EntityUid userUid)
        {
            userUid = UserUid;
        }
    }
}
