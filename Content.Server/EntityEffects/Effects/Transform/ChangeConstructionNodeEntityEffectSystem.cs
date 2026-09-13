using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.Transform;

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

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChangeConstructionNode : EntityEffectBase<ChangeConstructionNode>
{
    /// <summary>
    /// The construction node to change to.
    /// </summary>
    [DataField(required: true)]
    public string Node { get; set; } = string.Empty;
}
