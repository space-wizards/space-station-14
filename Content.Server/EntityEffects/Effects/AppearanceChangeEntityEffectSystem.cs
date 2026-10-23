using Content.Server.Construction;
using Content.Shared.Construction;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

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
