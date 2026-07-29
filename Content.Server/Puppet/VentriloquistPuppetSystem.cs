using Content.Server.Ghost.Roles.Components;
using Content.Server.Popups;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Puppet;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Puppet;

/// <summary>
/// A system for interactions with ventriloquist dummies, puppets that another player can talk through when equipped in a character's hand.
/// </summary>
public sealed partial class VentriloquistPuppetSystem : SharedVentriloquistPuppetSystem
{
    public static readonly EntProtoId MutedEffect = "StatusEffectVentriloquistPuppetMuted";

    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    /// <summary>
    /// When used user inserts hand into dummy and the dummy can speak, when used again the user removes hand
    /// from dummy and the dummy cannot speak.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<VentriloquistPuppetComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // TODO stop using mute component as a toggle for this component's functionality.
        // TODO disable dummy when the user dies or cannot interact.
        // Then again, this is all quite cursed code, so maybe its a cursed ventriloquist puppet.

        if (!_statusEffects.TryRemoveStatusEffect(uid, MutedEffect))
        {
            _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-remove-hand"), ent, args.User);
            MuteDummy(ent);
            return;
        }

        // TODO why does this need a combat component???
        EnsureComp<CombatModeComponent>(ent);
        _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-insert-hand"), ent, args.User);
        _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-inserted-hand"), ent, ent);

        if (!HasComp<GhostTakeoverAvailableComponent>(ent))
        {
            AddComp<GhostTakeoverAvailableComponent>(ent);
            var ghostRole = EnsureComp<GhostRoleComponent>(ent);
            ghostRole.RoleName = Loc.GetString("ventriloquist-puppet-role-name");
            ghostRole.RoleDescription = Loc.GetString("ventriloquist-puppet-role-description");
        }

        args.Handled = true;
    }

    /// <summary>
    /// When dropped the dummy is muted again.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDropped(Entity<VentriloquistPuppetComponent> ent, ref DroppedEvent args)
    {
        if (_statusEffects.HasStatusEffect(uid, MutedEffect))
            return;

        _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-remove-hand"), ent, args.User);
        MuteDummy(ent);
    }

    /// <summary>
    /// When unequipped from a hand slot the dummy is muted again.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUnequippedHand(Entity<VentriloquistPuppetComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_statusEffects.HasStatusEffect(uid, MutedEffect))
            return;

        _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-remove-hand"), ent, args.User);
        MuteDummy(ent);
    }

    /// <summary>
    /// Mutes the dummy.
    /// </summary>
    private void MuteDummy(Entity<VentriloquistPuppetComponent> ent)
    {
        _popupSystem.PopupEntity(Loc.GetString("ventriloquist-puppet-removed-hand"), ent, ent);
        _statusEffects.TrySetStatusEffectDuration(uid, MutedEffect);
        RemComp<CombatModeComponent>(ent);
        RemComp<GhostTakeoverAvailableComponent>(ent);
    }
}

