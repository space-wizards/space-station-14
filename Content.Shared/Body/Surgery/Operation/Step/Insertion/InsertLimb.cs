using Content.Shared.Body.Systems;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Operation.Step.Insertion;

public sealed class InsertLimb : IInsertionHandler
{
    public bool TryInsert(EntityUid part, EntityUid item)
    {
        var systemMan = IoCManager.Resolve<IEntitySystemManager>();
        var body = systemMan.GetEntitySystem<SharedBodySystem>();
        var entMan = IoCManager.Resolve<IEntityManager>();
        if (!entMan.TryGetComponent<BodyPartComponent>(item, out var child))
            return false;

        // attach the part to the first valid slot
        var parent = entMan.GetComponent<BodyPartComponent>(part);
        foreach (var (id, slot) in parent.Children)
        {
            // can't check for sidedness so 2 left hands is possible!!!
            if (slot.Child == null && slot.Type == child.PartType)
                return body.AttachPart(item, slot);
        }

        return false;
    }
}
