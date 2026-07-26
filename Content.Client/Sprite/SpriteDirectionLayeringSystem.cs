using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Reflection;

namespace Content.Client.Sprite;

public sealed partial class SpriteDirectionLayeringSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IReflectionManager _reflection = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        _spriteQuery = GetEntityQuery<SpriteComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SpriteDirectionLayeringComponent, TransformComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform, out var sprite))
        {
            if (!comp.DirtyOverrides)
                continue;

            RegenerateCachedOverrides((uid, comp));
            comp.DirtyOverrides = false;
        }
    }

    /// <summary>
    /// Marks an entity to have its cached layer overrides regenerated before rendering.
    /// Must be ran whenever new layers have been added or removed to ensure the indexes point to the correct layers.
    /// </summary>
    /// <param name="entity">The entity to update the overrides of.</param>
    public void DirtyCachedOverrides(Entity<SpriteDirectionLayeringComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.DirtyOverrides = true;
    }

    /// <summary>
    /// Tries to parse a key to an enum. Necessary since layer mapping takes both enums and strings as keys.
    /// </summary>
    private object ParseKey(string keyString)
    {
        if (_reflection.TryParseEnumReference(keyString, out var @enum))
            return @enum;

        return keyString;
    }

    private void RegenerateCachedOverrides(Entity<SpriteDirectionLayeringComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!_spriteQuery.TryComp(entity.Owner, out var sprite) || entity.Comp == null)
            return;

        foreach (var (direction, list) in entity.Comp.DirectionLayers)
        {
            List<int>? subList;

            if (!entity.Comp.CachedLayerOverrides.TryGetValue(direction, out subList))
                subList = new List<int>();
            else
                subList.Clear();

            for (var i = 0; i < list.Count; i++)
            {
                var layer = list[i];
                var key = layer.MapKeys?.FirstOrDefault();
                if (key == null)
                    continue;

                var parsedKey = ParseKey(key);

                if (parsedKey is Enum enumkey)
                {
                    if (_sprite.LayerMapTryGet((entity.Owner, sprite), enumkey, out var index, false))
                        subList.Add(index);
                }
                else if (parsedKey is string stringkey)
                {
                    if (_sprite.LayerMapTryGet((entity.Owner, sprite), stringkey, out var index, false))
                        subList.Add(index);
                }
            }

            entity.Comp.CachedLayerOverrides[direction] = subList;
        }

        _sprite.SetLayersOrderOverride((entity.Owner, sprite), entity.Comp.CachedLayerOverrides, entity.Comp.DirectionType);
    }
}
