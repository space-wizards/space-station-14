using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Emag.Systems
{
    /// How to add an emag interaction:
    /// 1. Go to the system for the component you want the interaction with
    /// 2. Subscribe to the GotEmaggedEvent
    /// 3. Have some check for if this actually needs to be emagged or is already emagged (to stop charge waste)
    /// 4. Past the check, add all the effects you desire and HANDLE THE EVENT ARGUMENT so a charge is spent
    public sealed class EmagSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmagComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<EmagComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<EmagComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<EmagComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<EmagComponent, EntityUnpausedEvent>(OnUnpaused);
        }

        private void OnGetState(EntityUid uid, EmagComponent component, ref ComponentGetState args)
        {
            args.State = new EmagComponentState(component.MaxCharges, component.Charges, component.RechargeDuration,
                component.NextChargeTime, component.EmagImmuneTag, component.AutoRecharge);
        }

        private void OnHandleState(EntityUid uid, EmagComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not EmagComponentState state)
                return;

            component.MaxCharges = state.MaxCharges;
            component.Charges = state.Charges;
            component.RechargeDuration = state.RechargeTime;
            component.NextChargeTime = state.NextChargeTime;
            component.EmagImmuneTag = state.EmagImmuneTag;
            component.AutoRecharge = state.AutoRecharge;
        }

        private void OnUnpaused(EntityUid uid, EmagComponent component, ref EntityUnpausedEvent args)
        {
            component.NextChargeTime += args.PausedTime;
        }

        private void OnExamine(EntityUid uid, EmagComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.Charges)));
            if (component.Charges == component.MaxCharges)
            {
                args.PushMarkup(Loc.GetString("emag-max-charges"));
                return;
            }
            var timeRemaining = Math.Round((component.NextChargeTime - _timing.CurTime).TotalSeconds);
            args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", timeRemaining)));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var emag in EntityQuery<EmagComponent>())
            {
                if (!emag.AutoRecharge)
                    continue;

                if (emag.Charges == emag.MaxCharges)
                    continue;

                if (_timing.CurTime < emag.NextChargeTime)
                    continue;

                ChangeEmagCharge(emag.Owner, 1, true, emag);
            }
        }

        private void OnAfterInteract(EntityUid uid, EmagComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach || args.Target is not { } target)
                return;

            args.Handled = TryUseEmag(uid, args.User, target, component);
        }

        /// <summary>
        /// Changes the charge on an emag.
        /// </summary>
        public bool ChangeEmagCharge(EntityUid uid, int change, bool resetTimer, EmagComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (component.Charges + change < 0 || component.Charges + change > component.MaxCharges)
                return false;

            if (resetTimer || component.Charges == component.MaxCharges)
                component.NextChargeTime = _timing.CurTime + component.RechargeDuration;

            component.Charges += change;
            Dirty(component);
            return true;
        }

        /// <summary>
        /// Tries to use the emag on a target entity
        /// </summary>
        public bool TryUseEmag(EntityUid emag, EntityUid user, EntityUid target, EmagComponent? component = null)
        {
            if (!Resolve(emag, ref component, false))
                return false;

            if (_tagSystem.HasTag(target, component.EmagImmuneTag))
                return false;

            if (component.Charges <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-no-charges"), user, user);
                return false;
            }

            var handled = DoEmagEffect(user, target);
            if (!handled)
                return false;

            // only do popup on client
            if (_net.IsClient && _timing.IsFirstTimePredicted)
            {
                _popupSystem.PopupEntity(Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))), user,
                    user, PopupType.Medium);
            }

            _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} emagged {ToPrettyString(target):target}");

            ChangeEmagCharge(emag, -1, false, component);
            return true;
        }

        /// <summary>
        /// Does the emag effect on a specified entity
        /// </summary>
        public bool DoEmagEffect(EntityUid user, EntityUid target)
        {
            var emaggedEvent = new GotEmaggedEvent(user);
            RaiseLocalEvent(target, ref emaggedEvent);
            return emaggedEvent.Handled;
        }
    }

    [ByRefEvent]
    public record struct GotEmaggedEvent(EntityUid UserUid, bool Handled = false);
}
