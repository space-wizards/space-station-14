using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;

namespace Content.Shared.RepulseAttract;

public sealed class RepulseAttractSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xForm = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RepulseAttractComponent, ActivateInWorldEvent>(OnActivate);
    }

    // TODO: Internal Cooldown should be added if no usedelay

    private void OnActivate(Entity<RepulseAttractComponent> ent, ref ActivateInWorldEvent args)
    {
        RepulseAttract(args.User, ent.Comp.Range, ent.Comp.Strength, ent.Comp.Attract, ent.Comp.Whitelist, ent.Comp.Blacklist);
    }

    // TODO: Backend code - entities in rage, check lists, do strength calculations
    // TODO: Public API (use system to call, can be re-used in things like grav anom)

    public void RepulseAttract(EntityUid caster, float range, float strength, bool attract, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        var xForm = Transform(caster);

        // Use lookup and range to check entities in range
        var entsInRange = _lookup.GetEntitiesInRange(caster, range);

        foreach (var target in entsInRange)
        {
            // Check white/black lists
            if (_whitelist.IsWhitelistFail(whitelist, target) || _whitelist.IsBlacklistFail(blacklist, target))
                continue;

            var targetXForm = Transform(target);

            if (HasComp<GhostComponent>(target) || targetXForm.Anchored)
                continue;

            var userWorldPos = _xForm.GetWorldPosition(xForm);
            var targetWorldPos = _xForm.GetWorldPosition(target);

            var direction = targetWorldPos - userWorldPos;

            if (attract)
                direction = userWorldPos - targetWorldPos;

            _throw.TryThrow(target, direction, strength, doSpin: true);
        }
    }
}
