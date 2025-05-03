using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.Effects;

[DataDefinition]
public sealed partial class CreateEntityReactionEffect : EntityEffect
{
    /// <summary>
    ///     What entity to create.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = default!;

    /// <summary>
    ///     How many entities to create per unit reaction.
    /// </summary>
    [DataField]
    public uint Number = 1;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-create-entity-reaction-effect",
            ("chance", Probability),
            ("entname", IoCManager.Resolve<IPrototypeManager>().Index<EntityPrototype>(Entity).Name),
            ("amount", Number));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<CreateEntityReactionEffect>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
