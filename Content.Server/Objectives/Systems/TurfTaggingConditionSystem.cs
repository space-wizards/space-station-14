using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Timing;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles counting department airlocks for the turf tagging objective.
/// </summary>
public sealed class TurfTaggingConditionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TurfWarRuleSystem _turfWar = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TurfTaggingConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<TurfTaggingConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        // update cached values if its expired
        var now = _timing.CurTime;
        if (now > ent.Comp.NextCache)
        {
            ent.Comp.NextCache = now + ent.Comp.CacheExpiry;
            UpdateCache(ent.Comp, args.MindId);
        }

        // prevent divide by zero
        if (ent.Comp.Best == 0)
        {
            args.Progress = 0;
            return;
        }

        args.Progress = (float) ent.Comp.Doors / (float) ent.Comp.Best;
    }

    private void UpdateCache(TurfTaggingConditionComponent comp, EntityUid mindId)
    {
        if (!TryComp<TurfTaggerRoleComponent>(mindId, out var role))
            return;

        // counting all airlocks is non negligible, so this is why its cached
        // to prevent someone spamming C from slowing the server down.
        var counts = _turfWar.CountAirlocks(role.Rule.Comp);

        // find the best department
        comp.Best = 0;
        foreach (var count in counts.Values)
        {
            if (count > comp.Best)
                comp.Best = count;
        }

        // find the doors that your department got
        comp.Doors = counts[role.Department];
    }
}
