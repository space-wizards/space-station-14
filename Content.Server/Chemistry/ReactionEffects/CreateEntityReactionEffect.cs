using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReactionEffects;

[DataDefinition]
public sealed partial class CreateEntityReactionEffect : ReagentEffect
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

    public override void Effect(ReagentEffectArgs args)
    {
        var transform = args.EntityManager.GetComponent<TransformComponent>(args.SolutionEntity);
        var transformSystem = args.EntityManager.System<SharedTransformSystem>();
        var quantity = Number * args.Quantity.Int();

        for (var i = 0; i < quantity; i++)
        {
            var uid = args.EntityManager.SpawnEntity(Entity, transform.MapPosition);
            transformSystem.AttachToGridOrMap(uid);

            // TODO figure out how to properly spawn inside of containers
            // e.g. cheese:
            // if the user is holding a bowl milk & enzyme, should drop to floor, not attached to the user.
            // if reaction happens in a backpack, should insert cheese into backpack.
            // --> if it doesn't fit, iterate through parent storage until it attaches to the grid (again, DON'T attach to players).
            // if the reaction happens INSIDE a stomach? the bloodstream? I have no idea how to handle that.
            // presumably having cheese materialize inside of your blood would have "disadvantages".
        }
    }
}
