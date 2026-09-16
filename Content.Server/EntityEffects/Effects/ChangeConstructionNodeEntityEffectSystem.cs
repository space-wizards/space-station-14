using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Changes the construction node of the entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChangeConstructionNodeEntityEffectSystem : EntityEffectSystem<ConstructionComponent, ChangeConstructionNode>
{
    [Dependency] private ConstructionSystem _construction = default!;

    protected override void Effect(Entity<ConstructionComponent> entity, ref EntityEffectEvent<ChangeConstructionNode> args)
    {
        if (string.IsNullOrEmpty(args.Effect.Node))
            return;

        _construction.ChangeNode(entity, null, args.Effect.Node, true, entity.Comp);
    }
}
