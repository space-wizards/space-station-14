using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Puppet;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Puppet
{
    public sealed partial class VentriloquistPuppetSystem : SharedVentriloquistPuppetSystem
    {
        public static readonly EntProtoId MutedEffect = "StatusEffectVentriloquistPuppetMuted";

        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private StatusEffectsSystem _statusEffects = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VentriloquistPuppetComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<VentriloquistPuppetComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<VentriloquistPuppetComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        }

        /// <summary>
        /// When used user inserts hand into dummy and the dummy can speak, when used again the user removes hand
        /// from dummy and the dummy cannot speak.
        /// </summary>
        private void OnUseInHand(EntityUid uid, VentriloquistPuppetComponent component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            // TODO stop using mute component as a toggle for this component's functionality.
            // TODO disable dummy when the user dies or cannot interact.
            // Then again, this is all quite cursed code, so maybe its a cursed ventriloquist puppet.

            if (!_statusEffects.TryRemoveStatusEffect(uid, MutedEffect))
            {
                _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-remove-hand"), uid, args.User);
                MuteDummy(uid, component);
                return;
            }

            // TODO why does this need a combat component???
            EnsureComp<CombatModeComponent>(uid);
            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-insert-hand"), uid, args.User);
            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-inserted-hand"), uid, uid);

            if (!HasComp<GhostTakeoverAvailableComponent>(uid))
            {
                AddComp<GhostTakeoverAvailableComponent>(uid);
                var ghostRole = EnsureComp<GhostRoleComponent>(uid);
                ghostRole.RoleName = Loc.GetString("ventriloquist-puppet-role-name");
                ghostRole.RoleDescription = Loc.GetString("ventriloquist-puppet-role-description");
            }

            args.Handled = true;
        }

        /// <summary>
        /// When dropped the dummy is muted again.
        /// </summary>
        private void OnDropped(EntityUid uid, VentriloquistPuppetComponent component, DroppedEvent args)
        {
            if (_statusEffects.HasStatusEffect(uid, MutedEffect))
                return;

            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-remove-hand"), uid, args.User);
            MuteDummy(uid, component);
        }

        /// <summary>
        /// When unequipped from a hand slot the dummy is muted again.
        /// </summary>
        private void OnUnequippedHand(EntityUid uid, VentriloquistPuppetComponent component, GotUnequippedHandEvent args)
        {
            if (_statusEffects.HasStatusEffect(uid, MutedEffect))
                return;

            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-remove-hand"), uid, args.User);
            MuteDummy(uid, component);
        }

        /// <summary>
        /// Mutes the dummy.
        /// </summary>
        private void MuteDummy(EntityUid uid, VentriloquistPuppetComponent component)
        {
            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-removed-hand"), uid, uid);
            _statusEffects.TrySetStatusEffectDuration(uid, MutedEffect);
            RemComp<CombatModeComponent>(uid);
            RemComp<GhostTakeoverAvailableComponent>(uid);
        }
    }
}

