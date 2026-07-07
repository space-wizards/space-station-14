using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Grants the objectives of <see cref="AutoObjectivesComponent"/> whenever a mind
/// enters the entity. Minds that already hold an objective of the same prototype
/// (e.g. after being transferred back into the body) don't get duplicates.
/// </summary>
public sealed class AutoObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoObjectivesComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, AutoObjectivesComponent comp, MindAddedMessage args)
    {
        var (mindId, mind) = args.Mind;
        foreach (var proto in comp.Objectives)
        {
            if (HasObjective(mind, proto))
                continue;

            if (!_mind.TryAddObjective(mindId, mind, proto))
                Log.Warning($"Failed to add objective {proto} from {ToPrettyString(uid)} to mind {ToPrettyString(mindId)}");
        }
    }

    private bool HasObjective(MindComponent mind, EntProtoId proto)
    {
        foreach (var objective in mind.Objectives)
        {
            if (MetaData(objective).EntityPrototype?.ID == proto.Id)
                return true;
        }

        return false;
    }
}
