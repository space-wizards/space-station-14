using Content.Server.Construction;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.Construction;

/// <summary>
/// Adds a container to the entity.
/// </summary>
public sealed partial class AddContainerEntityEffectSystem : EntityEffectSystem<TransformComponent, AddContainer>
{
    [Dependency] private ConstructionSystem _construction = default!;

    /// <inheritdoc/>
    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<AddContainer> args)
    {
        _construction.AddContainer(entity, args.Effect.Container);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddContainer : EntityEffectBase<AddContainer>
{
    [DataField(required: true)]
    public string Container;
}
