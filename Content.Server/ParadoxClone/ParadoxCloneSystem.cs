using Content.Shared.Mind;
using Robust.Shared.Timing;

namespace Content.Server.ParadoxClone;

public sealed partial class ParadoxCloneSystem : EntitySystem
{
    [Dependency]
    private SharedMindSystem _mindSystem = default!;
    [Dependency]
    private SharedTransformSystem _transformSystem = default!;
    [Dependency]
    private IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParadoxCloneComponent, ActionParadoxCloneMaterializeEvent>(OnMaterialize);
    }

    /// <summary>
    /// Materializes the paradox clone, removing its ghost entity and spawning its real body.
    /// </summary>
    private void OnMaterialize(Entity<ParadoxCloneComponent> ent, ref ActionParadoxCloneMaterializeEvent args)
    {
        // get the mind to transfer
        if (!_mindSystem.TryGetMind(args.Performer, out var mind, out var mindComp))
            return;

        // transfer the mind and retrieve the body from nullspace
        _mindSystem.TransferTo(mind, ent.Comp.ClonedBody);
        _transformSystem.SetMapCoordinates(ent.Comp.ClonedBody, _transformSystem.GetMapCoordinates(ent.Owner));

        // Finally, delete the ghost entity
        _entMan.DeleteEntity(ent.Owner);
    }
}
