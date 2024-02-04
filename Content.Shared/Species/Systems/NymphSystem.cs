using Content.Shared.Species.Components;
using Content.Shared.Body.Events;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Species;

public sealed partial class NymphSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NymphComponent, RemovedFromPartInBodyEvent>(OnRemovedFromPart);
    }

    private void OnRemovedFromPart(EntityUid uid, NymphComponent comp, RemovedFromPartInBodyEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.OldBody))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(comp.EntityPrototype, out var entityProto))
            return;

        var coords = Transform(uid).Coordinates;
        var nymph = EntityManager.SpawnEntity(entityProto.ID, coords);

        if (comp.TransferMind == true && _mindSystem.TryGetMind(args.OldBody, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, nymph, mind: mind);

        EntityManager.QueueDeleteEntity(uid);
    }
}
