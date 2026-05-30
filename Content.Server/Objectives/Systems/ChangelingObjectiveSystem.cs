using System.Text;
using Content.Server.Changeling.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Systems;

public sealed partial class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingMindIdentityTrackerComponent, MindAgentTextAppendEvent>(OnAgentAppendText);

        SubscribeLocalEvent<ChangelingUniqueIdentityConditionComponent, ObjectiveGetProgressEvent>(OnGetUniqueIdentitiesProgress);

        SubscribeLocalEvent<ChangelingDevourMostConditionComponent, ObjectiveGetProgressEvent>(OnGetMostIdentitiesProgress);

        SubscribeLocalEvent<ChangelingDevouredEvent>(OnChangelingDevoured);
        SubscribeLocalEvent<ChangelingGainedIdentityEvent>(OnChangelingGainedIdentity);
    }

    private void OnAgentAppendText(Entity<ChangelingMindIdentityTrackerComponent> ent, ref MindAgentTextAppendEvent args)
    {
        if (ent.Comp.AppendIssuer == null)
            return;
        
        if (args.Issuer != ent.Comp.AppendIssuer)
            return;

        var summary = new StringBuilder();

        summary.AppendLine(Loc.GetString("changeling-round-end-identities-category"));
        summary.AppendLine(Loc.GetString("changeling-round-end-identities-text", ("count", ent.Comp.Identities.Count)));

        foreach (var data in ent.Comp.Identities)
        {
            summary.AppendLine(Loc.GetString("changeling-round-end-identities-wrapper", ("name", data.OriginalName), ("devoured", data.GrantedDna)));
        }

        args.Text += summary.ToString();
    }

    private void OnChangelingDevoured(ref ChangelingDevouredEvent args)
    {
        if (!_mind.TryGetMind(args.Changeling, out var mind, out _))
            return;

        // We add the identity to the list of tracked identities on the mind.
        // This can then be used by objectives to determine the amount of obtained identities, as well as if they were gained via Devour.
        AddOrUpdateUniqueIdentityToTracker(mind, args.Devoured, args.GrantedDna);
    }

    private void OnChangelingGainedIdentity(ref ChangelingGainedIdentityEvent args)
    {
        if (!_mind.TryGetMind(args.Changeling, out var mind, out _))
            return;

        if (args.Identity.Original == null)
            return;

        // We add the identity to the list of tracked identities on the mind.
        // This can then be used by objectives to determine the amount of obtained identities, as well as if they were gained via Devour.
        AddOrUpdateUniqueIdentityToTracker(mind, args.Identity.Original.Value, args.Identity.GrantedDna);
    }

    private void OnGetUniqueIdentitiesProgress(Entity<ChangelingUniqueIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetUniqueIdentitiesProgress(args.MindId, _number.GetTarget(ent));
    }

    private void OnGetMostIdentitiesProgress(Entity<ChangelingDevourMostConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetMostIdentitiesProgress(args.MindId);
    }

    private float GetUniqueIdentitiesProgress(EntityUid mind, int target)
    {
        // We've never actually gained an identity.
        if (!TryComp<ChangelingMindIdentityTrackerComponent>(mind, out var tracker))
            return 0f;

        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        var uniqueCount = tracker.UniqueDevouredCount;

        if (uniqueCount >= target)
            return 1f;

        return (float)uniqueCount / (float)target;
    }

    private float GetMostIdentitiesProgress(EntityUid mind)
    {
        // Can't progress if we've never eaten anyone.
        if (!TryComp<ChangelingMindIdentityTrackerComponent>(mind, out var selfTracker))
            return 0f;

        // We never actually devoured anyone.
        // We don't want to grant greentext if 0 is technically the highest.
        if (selfTracker.UniqueDevouredCount is var selfUniqueCount && selfUniqueCount < 1)
            return 0f;

        var query = AllEntityQuery<ChangelingMindIdentityTrackerComponent>();

        int highest = 0;

        while (query.MoveNext(out var uid, out var tracker))
        {
            // Skip our own tracker. We only care for the highest others have.
            if (uid == mind)
                continue;

            if (tracker.UniqueDevouredCount > highest)
                highest = tracker.UniqueDevouredCount;
        }

        // No equal check. Only one can win.
        if (selfUniqueCount > highest)
            return 1f;

        // highest+1 because we aim to be 1 above the highest.
        return (float)selfUniqueCount / (float)(highest+1);
    }

    private void AddOrUpdateUniqueIdentityToTracker(EntityUid mind, EntityUid target, bool devoured)
    {
        EnsureComp<ChangelingMindIdentityTrackerComponent>(mind, out var tracker);

        // If the identity already exists, we just update if it was Devoured.
        // We check by EntityUid here because we still count paradox clones and such as unique devours.
        // Tracking by name alone would make it inconsistent to how devours are tracked by the ChangelingDevourSystem and ChangelingIdentitySystem.
        if (tracker.Identities.TryFirstOrDefault(x => x.Original == target, out var identity))
        {
            // We don't want to set it to False afterward, because this entity was Devoured at SOME point before.
            // So we either keep it the same, or mark is as true.
            identity.GrantedDna = identity.GrantedDna || devoured;
            return;
        }

        var newData = new ChangelingMindTrackedIdentityData()
        {
            Original = target,
            OriginalName = Name(target),
            GrantedDna = devoured,
        };

        tracker.Identities.Add(newData);
    }
}
