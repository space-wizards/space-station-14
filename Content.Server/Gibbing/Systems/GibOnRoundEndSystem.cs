using Content.Shared.Gibbing.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Server.Body.Systems;
using Content.Server.GameTicking;
using Content.Server.Objectives;
using Content.Shared.Inventory;

namespace Content.Server.Gibbing.Systems;
public sealed class GibOnRoundEndSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        // this is raised before ObjectivesSystem, so that survival objectives fail if it triggers (gibbing kills you)
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd, before: [typeof(ObjectivesSystem)]);
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        var gibQuery = EntityQueryEnumerator<GibOnRoundEndComponent>();

        // gib everyone with the component
        while (gibQuery.MoveNext(out var uid, out var gibComp))
        {
            var gib = false;
            // if they fulfill all objectives given in the component they are not gibbed
            if (_mind.TryGetMind(uid, out var mindId, out var mindComp))
            {
                foreach (var objectiveId in gibComp.PreventGibbingObjectives)
                {
                    if (!_mind.TryFindObjective((mindId, mindComp), objectiveId, out var objective)
                        || !_objectives.IsCompleted(objective.Value, (mindId, mindComp)))
                    {
                        gib = true;
                        break;
                    }
                }
            }
            else
                gib = true;

            if (!gib)
                continue;

            if (gibComp.DeleteItems)
            {
                var items = _inventory.GetHandOrInventoryEntities(uid);
                foreach (var item in items)
                {
                    PredictedQueueDel(item);
                }
            }

            if (gibComp.SpawnProto != null)
                SpawnAtPosition(gibComp.SpawnProto, Transform(uid).Coordinates);

            _body.GibBody(uid, splatModifier: 5f);
        }
    }
}
