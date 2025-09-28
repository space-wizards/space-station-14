using Content.Shared.Construction;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;

[DataDefinition]
public sealed partial class TriggerContainerContents : IGraphAction
{
    [DataField(required: true)]
    public string Container;

    [DataField]
    public string Key = TriggerSystem.DefaultTriggerKey;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var container = entityManager.System<SharedContainerSystem>();
        var trigger = entityManager.System<TriggerSystem>();
        if (!container.TryGetContainer(uid, Container, out var uidContainer))
            return;

        foreach (var entity in uidContainer.ContainedEntities)
        {
            trigger.Trigger(entity, userUid, Key);
        }
    }
}
