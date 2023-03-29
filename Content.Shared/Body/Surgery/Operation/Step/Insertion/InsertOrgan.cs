using Content.Shared.Body.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Operation.Step.Insertion;

public sealed class InsertOrgan : IInsertionHandler
{
    public bool TryInsert(EntityUid part, EntityUid item)
    {
        var systemMan = IoCManager.Resolve<IEntitySystemManager>();
        var body = systemMan.GetEntitySystem<SharedBodySystem>();
        return body.AddOrganToFirstValidSlot(item, part);
    }
}
