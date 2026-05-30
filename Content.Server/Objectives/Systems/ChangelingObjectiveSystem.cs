using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class ChangelingObjectiveSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingUniqueIdentityConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ChangelingDevouredEvent>(OnChangelingDevoured);
    }

    private void OnChangelingDevoured(ref ChangelingDevouredEvent args)
    {
        // We check if the devour granted us Dna.
        // We do this because we could already have gotten the identity by other means before, such as the Dna sting.
        // Dna grant basically ensures this is the first time valid devour has happened on a target.
        if (!args.GrantedDna)
            return;

        if (!_mind.TryGetObjectiveComp<ChangelingUniqueIdentityConditionComponent>(args.Changeling, out var obj))
            return;

        obj.UniqueIdentities++;
    }

    private void OnGetProgress(Entity<ChangelingUniqueIdentityConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent.Comp, _number.GetTarget(ent));
    }

    private float GetProgress(ChangelingUniqueIdentityConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (comp.UniqueIdentities >= target)
            return 1f;

        return (float)comp.UniqueIdentities / (float)target;
    }
}
