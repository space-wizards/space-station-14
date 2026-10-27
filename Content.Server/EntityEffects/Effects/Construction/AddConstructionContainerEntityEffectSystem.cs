using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Construction;

namespace Content.Server.EntityEffects.Effects.Construction;

/// <summary>
/// Adds a container to the constructionComponent in the entity.
/// </summary>
public sealed partial class AddConstructionContainerEntityEffectSystem : EntityEffectSystem<ConstructionComponent, AddConstructionContainer>
{
    [Dependency] private ConstructionSystem _construction = default!;

    /// <inheritdoc/>
    protected override void Effect(Entity<ConstructionComponent> entity, ref EntityEffectEvent<AddConstructionContainer> args)
    {
        _construction.AddContainer(entity, args.Effect.Container, entity.AsNullable());
    }
}
