using Content.Server.Mind;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Species.Components;
using Content.Shared.Zombies;
using Content.Shared.Mind.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Server.Species.Systems;

public sealed partial class NymphSystem : EntitySystem
{
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private ZombieSystem _zombie = default!;

    private EntityQuery<MindUntransferableToBrainComponent> _mindUntransferableQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NymphComponent, OrganGotRemovedEvent>(OnRemovedFromPart);

        _mindUntransferableQuery = GetEntityQuery<MindUntransferableToBrainComponent>();
    }

    private void OnRemovedFromPart(EntityUid uid, NymphComponent comp, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(uid) || TerminatingOrDeleted(args.Target))
            return;

        if (!ProtoMan.TryIndex<EntityPrototype>(comp.EntityPrototype, out var entityProto))
            return;

        // Get the organs' position & spawn a nymph there
        var coords = Transform(uid).Coordinates;
        var nymph = SpawnAtPosition(entityProto.ID, coords);

        if (HasComp<ZombieComponent>(args.Target)) // Zombify the new nymph if old one is a zombie
            _zombie.ZombifyEntity(nymph);

        // Move the mind if there is one and it's supposed to be transferred
        if (comp.TransferMind)
        {
            if (TryComp<MindContainerComponent>(uid, out var oldMindCont))
            {
                // A mind being moved from body -> brain counts as having inhabited the same container, even if the mind has since left.
                var nympMindCont = EnsureComp<MindContainerComponent>(nymph);
                _mindSystem.UpdateLatestMind((nymph, nympMindCont), oldMindCont.LatestMind);
            }

            if (_mindUntransferableQuery.HasComp(uid))
                AddComp<MindUntransferableToBrainComponent>(nymph);

            if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, nymph, mind: mind);
        }

        // Delete the old organ
        QueueDel(uid);
    }
}
