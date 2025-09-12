using Content.Shared.Projectiles;
using Content.Shared.Cover.Components;
using Robust.Shared.Random;
using Content.Shared.Examine;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Content.Shared.Physics;
using Content.Shared.Damage.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Cover;

public sealed class SharedCoverSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _coverExamineEnabled;

    private EntityQuery<FixturesComponent> _fixQuery;
    private EntityQuery<RequireProjectileTargetComponent> _reqTargetQuery;

    public override void Initialize()
    {
        base.Initialize();

        _reqTargetQuery = GetEntityQuery<RequireProjectileTargetComponent>();
        _fixQuery = GetEntityQuery<FixturesComponent>();

        SubscribeLocalEvent<CoverComponent, ProjectileMissCoverAttemptEvent>(OnMissCover);
        SubscribeLocalEvent<CoverComponent, ExaminedEvent>(OnExamine);

        Subs.CVar(_configuration, CCVars.CoverExamine, SetCoverExamine, true);
    }

    private void OnMissCover(Entity<CoverComponent> ent, ref ProjectileMissCoverAttemptEvent args)
    {
        if (TryMissCover(ent, args.Component, ent))
            args.Cancelled = true;
    }

    private bool TryMissCover(EntityUid projectile, ProjectileComponent comp, Entity<CoverComponent> cover)
    {
        // Maybe this should use comp.CreationTick instead? Adds another layer of unit conversion fuckery though.
        // relies on the firespeed being accurate. We have to poll speed at some time-point and that one seems better than backing it out at collision time.
        var traveltime = comp.FireTime != null ?
            _timing.CurTime - comp.FireTime :
            TimeSpan.MaxValue;

        var distance = comp.FireSpeed != null ?
            Math.Clamp(comp.FireSpeed.Value * (float)traveltime.Value.TotalSeconds, 0, cover.Comp.MaxDistance) :
            cover.Comp.MaxDistance;

        if (distance < cover.Comp.MinDistance) // we are too close and could shoot over easily
            return true;

        // extend the cover falloff range for guns with scopes.
        var maxDistAdj = cover.Comp.MaxDistance + comp.CoverRangeBonus;

        var mix = (maxDistAdj - distance) / maxDistAdj;
        var coverPctAdj = MathHelper.Lerp(cover.Comp.CoverPct, 0, mix); // closer means easier to miss cover
        if (!_random.Prob(coverPctAdj)) // we are too far to reach over easily
        {
            // don't need to consider penetration. Things that pen can miss cover, or hit it and let pen figure it out.
            return true;
        }

        return false;
    }

    // ToDo: consider hitscans.

    private void OnExamine(EntityUid ent, CoverComponent component, ref ExaminedEvent args)
    {
        if (!component.ShowExamine)
            return;

        if (!_coverExamineEnabled)
            return;

        if (!args.IsInDetailsRange)
            return;

        if (!_reqTargetQuery.TryComp(ent, out var req)) // It will always fly over
        {
            if (req != null && req.Active == true)
            {
                args.PushMarkup(Loc.GetString("no-cover"));
                return;
            }
        }

        // check if we can even collide with a projectile
        if (!_fixQuery.TryComp(ent, out var fix))
            return;

        var matches = false;
        var projlayers = CollisionGroup.Impassable | CollisionGroup.BulletImpassable;
        foreach (var f in fix.Fixtures.Values)
        {
            var layer = (CollisionGroup)f.CollisionLayer;
            if ((layer & projlayers) != 0)
            {
                matches = true;
                break;
            }
        }
        if (!matches)
        {
            args.PushMarkup(Loc.GetString("no-cover"));
            return;
        }

        // print some fuzzy text ~
        // not exact since the exact value will be meaningless and muddied in computation
        switch (component.CoverPct)
        {
            case <= 0f:
                args.PushMarkup(Loc.GetString("no-cover"));
                break;
            case <= 0.25f:
                args.PushMarkup(Loc.GetString("some-cover"));
                break;
            case <= 0.50f:
                args.PushMarkup(Loc.GetString("mild-cover"));
                break;
            case <= 0.75f:
                args.PushMarkup(Loc.GetString("good-cover"));
                break;
            default:
                args.PushMarkup(Loc.GetString("great-cover"));
                break;
        }

    }

    private void SetCoverExamine(bool val)
    {
        _coverExamineEnabled = val;
    }

}
