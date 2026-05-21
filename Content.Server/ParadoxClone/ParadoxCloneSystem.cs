using Content.Shared.ParadoxClone;

namespace Content.Server.ParadoxClone;

public sealed partial class ParadoxCloneSystem : SharedParadoxCloneSystem
{
    protected override void Materialize(Entity<ParadoxCloneComponent> ent)
    {
        base.Materialize(ent);

        // this part happens on the server since we need access to the clonedbody
        // get the mind to transfer
        if (!_mindSystem.TryGetMind(ent.Owner, out var mind, out var mindComp))
            return;

        // unpause the clone, who was paused so that it doesnt die of spacing
        SetPaused(ent.Comp.ClonedBody, false);

        // transfer the mind and retrieve the body from nullspace
        _mindSystem.TransferTo(mind, ent.Comp.ClonedBody);
        _transformSystem.SetMapCoordinates(ent.Comp.ClonedBody, _transformSystem.GetMapCoordinates(ent.Owner));

        // Finally, delete the ghost entity (we dont need to set IsWandering off because that component will be deleted alongside its entity)
        _entMan.DeleteEntity(ent.Owner);
    }
}
