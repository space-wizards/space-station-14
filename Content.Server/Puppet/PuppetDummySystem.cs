using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Puppet;
using Content.Shared.Hands.Components;
using Content.Server.Speech.Muting;
using Content.Shared.CombatMode;
using Content.Shared.Hands;

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

        /// <summary>
        /// When used user inserts hand into dummy and the dummy can speak, when used again the user removes hand
        /// from dummy and the dummy cannot speak.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="component"></param>
        /// <param name="args"></param>
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
                _popupSystem.PopupEntity(Loc.GetString("dummy-remove-hand"), uid, args.User);
                MuteDummy(uid, component);
            }

            args.Handled = true;
        }

        /// <summary>
        /// When dropped the dummy is muted again.
        /// </summary>
        private void OnDropped(EntityUid uid, PuppetDummyComponent component, DroppedEvent args)
        {
            if (HasComp<MutedComponent>(uid))
                return;

            _popupSystem.PopupEntity(Loc.GetString("dummy-remove-hand"), uid, args.User);
            MuteDummy(uid, component);
        }

        /// <summary>
        /// When unequipped from a hand slot the dummy is muted again.
        /// </summary>
        private void OnUnequippedHand(EntityUid uid, PuppetDummyComponent component, GotUnequippedHandEvent args)
        {
            if (HasComp<MutedComponent>(uid))
                return;

            _popupSystem.PopupEntity(Loc.GetString("dummy-remove-hand"), uid, args.User);
            MuteDummy(uid, component);
        }

        /// <summary>
        /// Mutes the dummy.
        /// </summary>
        private void MuteDummy(EntityUid uid, PuppetDummyComponent component)
        {
            _popupSystem.PopupEntity(Loc.GetString("dummy-removed-hand"), uid, uid);
            AddComp<MutedComponent>(uid);
            RemComp<CombatModeComponent>(uid);
        }
    }
}

