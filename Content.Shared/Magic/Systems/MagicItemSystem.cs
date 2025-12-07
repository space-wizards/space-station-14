using System;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Events;
using Robust.Shared.Map;

namespace Content.Shared.Magic.Systems;

public sealed class MagicItemSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    [Dependency] private readonly Content.Shared.CombatMode.SharedCombatModeSystem _combatMode = default!;

    [Dependency] private readonly Robust.Shared.Network.INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagicItemComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MagicItemComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionComponent, ProjectileSpellEvent>(OnActionProjectile);
        SubscribeLocalEvent<MagicItemComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnMapInit(Entity<MagicItemComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Action))
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);

        // If an action entity was created, override its event/properties as configured on the item.
        if (ent.Comp.ActionEntity is not { } actionEntity)
            return;

        // Override action use delay if configured (use the SharedActions API so access rules are respected)
        if (ent.Comp.UseDelaySeconds > 0f)
        {
            _actions.SetUseDelay(actionEntity, TimeSpan.FromSeconds(ent.Comp.UseDelaySeconds));
        }

        // Set the world-target event prototype so that the shared magic system will spawn the proper projectile.
        var ev = new ProjectileSpellEvent { Prototype = ent.Comp.Projectile };
        _actions.SetEvent(actionEntity, ev);
    }

    private void OnShutdown(Entity<MagicItemComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity is not { } action)
            return;

        // Avoid calling QueueDel on networked entities from the client - that triggers prediction errors.
        // If the action entity is client-side-only, delete it locally. If we're the server, queue deletion normally.
        if (IsClientSide(action))
        {
            Del(action);
        }
        else if (_netMan.IsServer)
        {
            QueueDel(action);
        }
    }

    private void OnActionProjectile(EntityUid actionUid, ActionComponent actionComp, ref ProjectileSpellEvent ev)
    {
        if (ev.Handled)
            return;

        // The action component's Container field points to the provider (the item that created the action).
        var provider = actionComp.Container;
        if (provider == null)
            return;

        if (!TryComp<MagicItemComponent>(provider.Value, out var magic))
            return;

        // Only allow use while performer is in combat mode (harm mode)
        if (!_combatMode.IsInCombatMode(ev.Performer))
        {
            ev.Handled = true;
            return;
        }

        // Ensure we fire the projectile configured on the item (override action prototype value)
        ev.Prototype = magic.Projectile;
    }

    private void OnGetActions(Entity<MagicItemComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.ActionEntity == null)
            return;

        args.AddAction(ent.Comp.ActionEntity.Value);
    }
}
