using Content.Server.Ghost.Roles.Components;
using Content.Server.Instruments;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Dummy;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Content.Shared.Hands.Components;
using Content.Server.Speech.Muting;
using Content.Shared.Item;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Server.Abilities.Mime;
using Robust.Shared.Audio;
using Content.Server.Magic.Events;
using Content.Server.Chat.Systems;
using Content.Shared.Stealth.Components;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Dummy
{
    public sealed class DummySystem : SharedDummySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DummyComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<DummyComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<DummyComponent, TeleportDummyActionEvent>(OnTeleport);
        }

        private void OnUseInHand(EntityUid uid, DummyComponent component, UseInHandEvent args)
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

            else if (HasComp<MutedComponent>(uid))
            {
            }

            args.Handled = true;
        }

        private void OnDropped(EntityUid uid, DummyComponent component, DroppedEvent args)
        {
            if (args.Handled)
                return;

            if (HasComp<MutedComponent>(uid))
                return;

            AddComp<MutedComponent>(uid);
            RemComp<CombatModeComponent>(uid);
            _actionsSystem.AddAction(uid, component.TeleportDummyAction, uid);


            args.Handled = true;
        }

        private void OnTeleport(EntityUid uid, DummyComponent component, TeleportDummyActionEvent args)
        {
            if (!component.Enabled)
                return;

            AddComp<StealthComponent>(uid);
            AddComp<StealthOnMoveComponent>(uid);

            args.Handled = true;
        }
    }
}

