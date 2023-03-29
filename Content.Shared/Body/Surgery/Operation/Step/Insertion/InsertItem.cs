using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery.Operation.Step.Insertion;

public sealed class InsertItem : IInsertionHandler
{
    /// <summary>
    /// Maximum size, inclusive, of an item that can be inserted.
    /// </summary>
    [DataField("maxSize")]
    public int MaxSize = 9999;

    public bool TryInsert(EntityUid part, EntityUid item)
    {
        var systemMan = IoCManager.Resolve<IEntitySystemManager>();
//        var containerMan = systemMan.GetEntitySystem<ContainerManagerSystem>();
        // TODO: add to bodypart container "bodypart", checking total size somehow
        return true;
    }
}
