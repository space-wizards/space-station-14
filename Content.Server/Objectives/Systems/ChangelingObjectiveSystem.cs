using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingUniqueIdentityConditionComponent, ObjectiveGetProgressEvent>(OnGetUniqueIdentitiesProgress);

        SubscribeLocalEvent<ChangelingDevourMostConditionComponent, ObjectiveGetProgressEvent>(OnGetMostIdentitiesProgress);
        SubscribeLocalEvent<ChangelingDevourMostConditionComponent, ObjectiveAssignedEvent>(OnMostIdentitiesAssigned);

        SubscribeLocalEvent<ChangelingDevouredEvent>(OnChangelingDevoured);
    }

    private void OnChangelingDevoured(ref ChangelingDevouredEvent args)
    {
        // We check if the devour granted us Dna.
        // We do this because we could already have gotten the identity by other means before, such as the Dna sting.
        // Dna grant basically ensures this is the first time valid devour has happened on a target.
        if (!args.GrantedDna)
            return;

        if (_mind.TryGetObjectiveComp<ChangelingUniqueIdentityConditionComponent>(args.Changeling, out var uniqueIdentityObj))
            uniqueIdentityObj.UniqueIdentities++;

        if (_mind.TryGetObjectiveComp<ChangelingDevourMostConditionComponent>(args.Changeling, out var devourMostObj))
            devourMostObj.UniqueIdentities++;
    }

    private void OnGetUniqueIdentitiesProgress(Entity<ChangelingUniqueIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetUniqueIdentitiesProgress(ent.Comp, _number.GetTarget(ent));
    }

    private void OnGetMostIdentitiesProgress(Entity<ChangelingDevourMostConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetMostIdentitiesProgress(ent);
    }

    private void OnMostIdentitiesAssigned(Entity<ChangelingDevourMostConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var query = AllEntityQuery<ChangelingDevourMostConditionComponent>();

        // Need at least 1 more changeling with this objective.
        while (query.MoveNext(out var uid, out _))
        {
            if (uid != ent.Owner)
            {
                args.Cancelled = false;
                return;
            }
        }

        args.Cancelled = true;
    }

    private float GetUniqueIdentitiesProgress(ChangelingUniqueIdentityConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (comp.UniqueIdentities >= target)
            return 1f;

        return (float)comp.UniqueIdentities / (float)target;
    }

    private float GetMostIdentitiesProgress(Entity<ChangelingDevourMostConditionComponent> ent)
    {
        var query = AllEntityQuery<ChangelingDevourMostConditionComponent>();

        int highest = 0;

        while (query.MoveNext(out var uid, out var devourMostComp))
        {
            if (uid == ent.Owner)
                continue;

            if (devourMostComp.UniqueIdentities > highest)
                highest = devourMostComp.UniqueIdentities;
        }

        // No equal check. Only one can win.
        if (ent.Comp.UniqueIdentities > highest)
            return 1f;

        // highest+1 because we aim to be 1 above the highest.
        return (float)ent.Comp.UniqueIdentities / (float)(highest+1);
    }
}
