using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Shared.RepulseAttract;

public sealed class RepulseAttractSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xForm = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RepulseAttractComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RepulseAttractComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<RepulseAttractComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnPickupAttempt(Entity<RepulseAttractComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled || ent.Owner == args.User)
            return;

        ent.Comp.User = args.User;
    }

    private void OnDrop(Entity<RepulseAttractComponent> ent, ref DroppedEvent args)
    {
        ent.Comp.User = null;
    }

    private void OnActivate(Entity<RepulseAttractComponent> ent, ref ActivateInWorldEvent args)
    {
        TryRepulseAttract(ent);
    }

    /// <summary>
    ///     Try to <see cref="RepulseAttract"/>, mostly checks for cooldown. If a user is provided, it will override the entity being used. Used in situations where there's items using the comp in inventory/hands.
    /// </summary>
    /// <param name="ent">The entity with Repulse Attract</param>
    /// <param name="user">Optional user, used for using an item with Repulse/Attract</param>
    /// <param name="singleTarget">Optional target, used for a single target click instead of in an area</param>
    /// <returns></returns>
    private bool TryRepulseAttract(Entity<RepulseAttractComponent> ent, EntityUid? singleTarget = null)
    {
        var caster = ent.Owner;
        var comp = ent.Comp;

        if (comp.User != null)
            caster = comp.User.Value;

        var start = _gameTiming.CurTime;

        if (comp.NextUse != null && start < comp.NextUse)
        {
            _popup.PopupClient(Loc.GetString("repulseattract-cooldown-active", ("cd", (comp.NextUse.Value.Seconds - start.Seconds))), caster);
            return false;
        }

        comp.NextUse = comp.UseDelay + start;

        if (comp.User != null)
            caster = comp.User.Value;

        var xForm = Transform(caster);

        // No range check, target was clicked on
        if (singleTarget != null)
        {
            RepulseAttractHelper(xForm, singleTarget.Value, ent);
            return true;
        }

        var entsInRange = _lookup.GetEntitiesInRange(caster, comp.Range);

        foreach (var target in entsInRange)
        {
            RepulseAttractHelper(xForm, target, ent);
        }

        return true;
    }

    /// <summary>
    ///     Backend code for <see cref="RepulseAttract"/>
    /// </summary>
    private bool RepulseAttractHelper(TransformComponent xform, EntityUid target, Entity<RepulseAttractComponent> repulseAttract)
    {
        var comp = repulseAttract.Comp;

        if (_whitelist.IsWhitelistFail(comp.Whitelist, target) || _whitelist.IsBlacklistFail(comp.Blacklist, target))
            return false;

        var targetXForm = Transform(target);

        if (targetXForm.Anchored || HasComp<GhostComponent>(target))
            return false;

        var userWorldPos = _xForm.GetWorldPosition(xform);
        var targetWorldPos = _xForm.GetWorldPosition(target);

        var direction = targetWorldPos - userWorldPos;

        if (comp.Attract)
            direction = userWorldPos - targetWorldPos;

        _throw.TryThrow(target, direction, comp.Strength, doSpin: true);

        return true;
    }
}
