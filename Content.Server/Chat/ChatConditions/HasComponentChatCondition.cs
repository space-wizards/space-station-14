using Content.Shared.Chat;

namespace Content.Server.Chat.ChatConditions;

/// <summary>
/// Checks if the consumer is on a map with an active server, if necessary.
/// </summary>
[DataDefinition]
public sealed partial class HasComponentChatCondition : EntityChatConditionBase
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    [DataField]
    public string? Component;

    protected override bool Check(EntityUid subjectEntity, ChatMessageContext chatContext)
    {
        if (Component == null)
            return false;

        IoCManager.InjectDependencies(this);

        var comp = _componentFactory.GetRegistration(Component, true);
        return _entityManager.HasComponent(subjectEntity, comp.Type);
    }
}
