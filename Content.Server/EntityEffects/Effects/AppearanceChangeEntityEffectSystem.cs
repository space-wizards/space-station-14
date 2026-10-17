using Content.Server.Construction;
using Content.Shared.Construction;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Changes the appearance of the entity.
/// </summary>
public sealed partial class AppearanceChangeEntityEffectSystem : EntityEffectSystem<TransformComponent, AppearanceChange>
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ConstructionSystem _construction = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<AppearanceChange> args)
    {
        if (!TryComp<AppearanceComponent>(entity, out var appearance))
            return;

        var data = args.Effect.Data;
        var key = args.Effect.Key;

        if (data is not null)
        {
            _appearance.SetData(entity, key, data, appearance);
            return;
        }

        var (node, edge) = _construction.GetCurrentNodeAndEdge(entity);
        var nodeName = edge?.Target ?? node?.Name;

        if (nodeName != null)
            _appearance.SetData(entity, key, nodeName, appearance);
    }
}

public sealed partial class AppearanceChange : EntityEffectBase<AppearanceChange>
{
    /// <summary>
    /// The appearance key to use.
    /// </summary>
    [DataField]
    public Enum Key = ConstructionVisuals.Key;

    /// <summary>
    /// The enum data to set. If not specified, will set the data to the name of the current edges' target node
    /// (or the current node). This is because appearance changes are usually associated with reaching a new node.
    /// </summary>
    [DataField]
    public Enum? Data;
}
