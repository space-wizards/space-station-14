using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Shared.RepulseAttract;

public sealed class RepulseAttractSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xForm = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RepulseAttractComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RepulseAttractComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<RepulseAttractComponent, AttemptMeleeEvent>(OnMeleeAttempt);
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

    private void OnMeleeAttempt(Entity<RepulseAttractComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.DisableDuringUseDelay)
        {
            if (TryComp<UseDelayComponent>(ent.Owner, out var useDelay) && _delay.IsDelayed((ent.Owner, useDelay)))
                return;
        }

        TryRepulseAttract(ent);
    }

    /// <summary>
    ///     Try to Repulse or Attract
    /// </summary>
    /// <returns></returns>
    private bool TryRepulseAttract(Entity<RepulseAttractComponent> ent)
    {
        var caster = ent.Owner;
        var comp = ent.Comp;

        if (comp.User != null)
            caster = comp.User.Value;

        var xForm = Transform(caster);

        var entsInRange = _lookup.GetEntitiesInRange(caster, comp.Range);

        foreach (var target in entsInRange)
        {
            if (_whitelist.IsWhitelistFail(comp.Whitelist, target) || _whitelist.IsBlacklistPass(comp.Blacklist, target))
                continue;

            var targetXForm = Transform(target);

            if (targetXForm.Anchored || HasComp<GhostComponent>(target))
                continue;

            var userWorldPos = _xForm.GetWorldPosition(xForm);
            var targetWorldPos = _xForm.GetWorldPosition(target);

            var direction = targetWorldPos - userWorldPos;

            if (comp.Attract)
                direction = userWorldPos - targetWorldPos;

            _throw.TryThrow(target, direction, comp.Strength, doSpin: true);
        }

        return true;
    }
}
