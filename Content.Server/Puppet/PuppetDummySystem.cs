using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Puppet;
using Content.Shared.Hands.Components;
using Content.Server.Speech.Muting;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Shared.Stealth.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Server.Mind.Components;

namespace Content.Server.Puppet
{
    public sealed class PuppetDummySystem : SharedPuppetDummySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PuppetDummyComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<PuppetDummyComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PuppetDummyComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        }

        private void OnUseInHand(EntityUid uid, PuppetDummyComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            var userHands = Comp<HandsComponent>(args.User);

            if (userHands.ActiveHandEntity == uid && HasComp<MutedComponent>(uid))
            {
                RemComp<MutedComponent>(uid);
                _popupSystem.PopupEntity(Loc.GetString("dummy-insert-hand"), uid, args.User);
                _popupSystem.PopupEntity(Loc.GetString("dummy-inserted-hand"), uid, uid);
                AddComp<CombatModeComponent>(uid);

                if (!HasComp<GhostTakeoverAvailableComponent>(uid))
                {
                    EnsureComp<GhostTakeoverAvailableComponent>(uid);
                    var ghostRole = AddComp<GhostRoleComponent>(uid);
                    ghostRole.RoleName = Loc.GetString("dummy-role-name");
                    ghostRole.RoleDescription = Loc.GetString("dummy-role-description");
                }

            }

            else if (userHands.ActiveHandEntity == uid && !HasComp<MutedComponent>(uid))
            {
                AddComp<MutedComponent>(uid);
                _popupSystem.PopupEntity(Loc.GetString("dummy-remove-hand"), uid, args.User);
                _popupSystem.PopupEntity(Loc.GetString("dummy-removed-hand"), uid, uid);
                RemComp<CombatModeComponent>(uid);
            }

            args.Handled = true;
        }

        private void OnDropped(EntityUid uid, PuppetDummyComponent component, DroppedEvent args)
        {
            if (HasComp<MutedComponent>(uid))
                return;

            _popupSystem.PopupEntity(Loc.GetString("dummy-remove-hand"), uid, args.User);
            _popupSystem.PopupEntity(Loc.GetString("dummy-removed-hand"), uid, uid);
            AddComp<MutedComponent>(uid);
            RemComp<CombatModeComponent>(uid);
        }

        private void OnUnequippedHand(EntityUid uid, PuppetDummyComponent component, GotUnequippedHandEvent args)
        {
            if (HasComp<MutedComponent>(uid))
                return;

            _popupSystem.PopupEntity(Loc.GetString("dummy-remove-hand"), uid, args.User);
            _popupSystem.PopupEntity(Loc.GetString("dummy-removed-hand"), uid, uid);
            AddComp<MutedComponent>(uid);
            RemComp<CombatModeComponent>(uid);
            RemComp<UnremoveableComponent>(uid);
        }
    }
}

