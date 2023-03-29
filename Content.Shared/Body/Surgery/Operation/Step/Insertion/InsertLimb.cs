using Content.Shared.Body.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Operation.Insertion;

public sealed class InsertLimb : IInsertionHandler
{
    public bool TryInsert(EntityUid part, EntityUid item)
    {
        var systemMan = IoCManager.Resolve<IEntitySystemManager>();
        var body = systemMan.GetEntitySystem<SharedBodySystem>();
        // TODO
        return false;
    }
}
