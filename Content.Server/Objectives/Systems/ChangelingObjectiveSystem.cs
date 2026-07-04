using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    [SubscribeLocalEvent]
    private void OnChangelingDevoured(ref ChangelingDevouredEvent args)
    {
        if (!_mind.TryGetMind(args.Changeling, out var mind, out _))
            return;

        if (!args.GrantedDna)
            return;

        EnsureComp<ChangelingMindIdentityTrackerComponent>(mind, out var tracker);
        tracker.Devoured++;
    }

    [SubscribeLocalEvent]
    private void OnGetUniqueIdentitiesProgress(Entity<ChangelingUniqueIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetUniqueIdentitiesProgress(args.MindId, _number.GetTarget(ent));
    }

    [SubscribeLocalEvent]
    private void OnGetMostIdentitiesProgress(Entity<ChangelingDevourMostConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetMostIdentitiesProgress(args.MindId);
    }

    /// <summary>
    /// Returns the progress for <see cref="ChangelingUniqueIdentityConditionComponent"/>.
    /// Uses data stored on the mind.
    /// </summary>
    /// <returns>Objective progress, between 0 and 1.</returns>
    private float GetUniqueIdentitiesProgress(EntityUid mind, int target)
    {
        // We've never actually gained an identity.
        if (!TryComp<ChangelingMindIdentityTrackerComponent>(mind, out var tracker))
            return 0f;

        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        var uniqueCount = tracker.Devoured;

        if (uniqueCount >= target)
            return 1f;

        return (float)uniqueCount / (float)target;
    }

    /// <summary>
    /// Returns the progress for <see cref="ChangelingDevourMostConditionComponent"/> for a given mind.
    /// Uses data stored on the mind.
    /// </summary>
    /// <returns>Objective progress, between 0 and 1.</returns>
    private float GetMostIdentitiesProgress(EntityUid mind)
    {
        // Can't progress if we've never eaten anyone.
        if (!TryComp<ChangelingMindIdentityTrackerComponent>(mind, out var selfTracker))
            return 0f;

        // We never actually devoured anyone.
        // We don't want to grant greentext if 0 is technically the highest.
        if (selfTracker.Devoured is var selfUniqueCount && selfUniqueCount < 1)
            return 0f;

        var query = AllEntityQuery<ChangelingMindIdentityTrackerComponent>();

        int highest = 0;

        while (query.MoveNext(out var uid, out var tracker))
        {
            // Skip our own tracker. We only care for the highest others have.
            if (uid == mind)
                continue;

            if (tracker.Devoured > highest)
                highest = tracker.Devoured;
        }

        // No equal check. Only one can win.
        if (selfUniqueCount > highest)
            return 1f;

        // highest+1 because we aim to be 1 above the highest.
        return (float)selfUniqueCount / (float)(highest+1);
    }
}
