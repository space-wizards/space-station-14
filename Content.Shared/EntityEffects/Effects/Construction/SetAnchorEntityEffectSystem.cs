namespace Content.Shared.EntityEffects.Effects.Construction;

/// <summary>
/// Anchors / unanchors an entity based on value.
/// </summary>
public sealed partial class SetAnchorEntityEffectSystem : EntityEffectSystem<TransformComponent, SetAnchor>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SetAnchor> args)
    {
        if (entity.Comp.Anchored == args.Effect.Value)
            return;

        _transform.TryAnchor(entity.AsNullable(), args.Effect.Value);
    }
}

public sealed partial class SetAnchor : EntityEffectBase<SetAnchor>
{
    [DataField]
    public bool Value = true;
}
