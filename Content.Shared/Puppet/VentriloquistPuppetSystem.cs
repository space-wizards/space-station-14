using Content.Shared.Emoting;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Robust.Shared.Timing;

namespace Content.Shared.Puppet;

public sealed class VentriloquistPuppetSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentriloquistPuppetComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<VentriloquistPuppetComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<VentriloquistPuppetComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<VentriloquistPuppetComponent, EmoteAttemptEvent>(OnEmoteAttempt);
    }

    /// <summary>
    /// When used user inserts hand into dummy and the dummy can speak, when used again the user removes hand
    /// from dummy and the dummy cannot speak.
    /// </summary>
    private void OnUseInHand(Entity<VentriloquistPuppetComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.HasHandInserted)
            return;

        ent.Comp.HasHandInserted = true;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("ventriloquist-puppet-insert-hand"), ent.Owner, args.User);
        _popup.PopupEntity(Loc.GetString("ventriloquist-puppet-inserted-hand"), ent.Owner, ent.Owner);

        // To avoid a client failure when a prediction is incomplete.
        if (_timing.IsFirstTimePredicted)
            RemComp<MutedComponent>(ent.Owner);

        if (!HasComp<GhostTakeoverAvailableComponent>(ent.Owner))
        {
            EnsureComp<GhostTakeoverAvailableComponent>(ent.Owner);
            var ghostRole = EnsureComp<GhostRoleComponent>(ent.Owner);
            ghostRole.RoleName = Loc.GetString("ventriloquist-puppet-role-name");
            ghostRole.RoleDescription = Loc.GetString("ventriloquist-puppet-role-description");
        }

        args.Handled = true;
    }

    private void OnDropped(Entity<VentriloquistPuppetComponent> ent, ref DroppedEvent args)
    {
        if (!ent.Comp.HasHandInserted)
            return;

        ResetDummy(ent, args.User);
    }

    private void OnUnequippedHand(Entity<VentriloquistPuppetComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!ent.Comp.HasHandInserted)
            return;

        ResetDummy(ent, args.User);
    }

    private void OnEmoteAttempt(Entity<VentriloquistPuppetComponent> ent, ref EmoteAttemptEvent args)
    {
        args.Cancel();
    }

    /// <summary>
    /// Mutes the dummy and blocks its interactions.
    /// </summary>
    private void ResetDummy(Entity<VentriloquistPuppetComponent> ent, EntityUid user)
    {
        ent.Comp.HasHandInserted = false;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("ventriloquist-puppet-remove-hand"), ent.Owner, user);
        _popup.PopupEntity(Loc.GetString("ventriloquist-puppet-removed-hand"), ent.Owner, ent.Owner);
        EnsureComp<MutedComponent>(ent.Owner);
    }
}
