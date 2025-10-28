using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// A type of <see cref="EntityEffectBase{T}"/> for effects that spawn entities by prototype.
/// </summary>
/// <typeparam name="T">The entity effect inheriting this BaseEffect</typeparam>
/// <inheritdoc cref="EntityEffect"/>
public abstract partial class BaseSpawnEntityEntityEffect<T> : EntityEffectBase<T> where T : BaseSpawnEntityEntityEffect<T>
{
    /// <summary>
    /// Amount of entities we're spawning
    /// </summary>
    [DataField]
    public int Number = 1;

    /// <summary>
    /// Prototype of the entity we're spawning
    /// </summary>
    [DataField (required: true)]
    public EntProtoId Entity;

    /// <summary>
    /// Whether this spawning is predicted. Set false to not predict the spawn.
    /// Entities with animations or that have random elements when spawned should set this to false.
    /// </summary>
    [DataField]
    public bool Predicted = true;

    /// <inheritdoc cref="EntityEffect.Scaling"/>
    public override bool Scaling => true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-spawn-entity",
            ("chance", Probability),
            ("entname", IoCManager.Resolve<IPrototypeManager>().Index<EntityPrototype>(Entity).Name),
            ("amount", Number));
}
