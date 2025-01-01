using Content.Shared.Ghost;
using Content.Shared.Interaction;
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
        SubscribeLocalEvent<RepulseAttractComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(Entity<RepulseAttractComponent> ent, ref ActivateInWorldEvent args)
    {
        TryRepulseAttract(ent, args.User);
        // RepulseAttract(args.User, ent.Comp.Range, ent.Comp.Strength, ent.Comp.Attract, ent.Comp.Whitelist, ent.Comp.Blacklist);
    }

    /// <summary>
    ///     Try to <see cref="RepulseAttract"/>, mostly checks for cooldown. If a user is provided, it will override the entity being used. Used in situations where there's items using the comp in inventory.
    /// </summary>
    /// <param name="ent">The entity with Repulse Attract</param>
    /// <param name="user">Optional user, using an item with Repulse/Attract</param>
    /// <returns></returns>
    private bool TryRepulseAttract(Entity<RepulseAttractComponent> ent, EntityUid? user = null)
    {
        var caster = ent.Owner;

        if (user != null)
            caster = user.Value;

        var comp = ent.Comp;
        var start = _gameTiming.CurTime;

        if (comp.NextUse != null && start < comp.NextUse)
        {
            _popup.PopupClient(Loc.GetString("repulseattract-cooldown-active", ("cd", (comp.NextUse.Value.Seconds - start.Seconds))), caster);
            return false;
        }

        comp.NextUse = comp.UseDelay + start;
        RepulseAttract(caster, comp.Range, comp.Strength, comp.Attract, comp.Whitelist, comp.Blacklist);

        return true;
    }

    /// <summary>
    ///     Directly repulse/attract entities in range.
    /// </summary>
    /// <param name="caster">The entity performing the repulse/attract</param>
    /// <param name="range">How far this should reach</param>
    /// <param name="strength">How strong should it repulse/attract</param>
    /// <param name="attract">Should this attract instead of repulse?</param>
    /// <param name="whitelist">Entities allowed to be repulsed/attracted</param>
    /// <param name="blacklist">Entities not allowed to be repulsed/attracted</param>
    public void RepulseAttract(EntityUid caster, float range, float strength, bool attract, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        var xForm = Transform(caster);
        var entsInRange = _lookup.GetEntitiesInRange(caster, range);

        foreach (var target in entsInRange)
        {
            if (_whitelist.IsWhitelistFail(whitelist, target) || _whitelist.IsBlacklistFail(blacklist, target))
                continue;

            var targetXForm = Transform(target);

            if (targetXForm.Anchored || HasComp<GhostComponent>(target))
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
