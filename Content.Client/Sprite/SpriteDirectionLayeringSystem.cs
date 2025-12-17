using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Reflection;

namespace Content.Client.Sprite;

public sealed partial class SpriteDirectionLayeringSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<SpriteDirectionLayeringComponent> _spriteDirectionQuery;

    public override void Initialize()
    {
        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _spriteDirectionQuery = GetEntityQuery<SpriteDirectionLayeringComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        // This ensures the layer order override updates based on the entity's direction.
        // However this is technically calculated twice, as SpriteSystem.Render does the same to determine which sprite to use for directional sprites.
        // If you can figure out a way to not do this twice, let me know.
        var query = EntityQueryEnumerator<SpriteDirectionLayeringComponent, TransformComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform, out var sprite))
        {
            var angle = _xform.GetWorldRotation(uid) + _eyeManager.CurrentEye.Rotation; // angle on-screen. Used to decide the direction of 4/8 directional RSIs
            angle = angle.Reduced().FlipPositive();  // Reduce the angles to fix math shenanigans

            var direction = SpriteComponent.Layer.GetDirection(RsiDirectionType.Dir4, angle);

            if (direction == comp.PreviousDirection)
                continue;

            comp.PreviousDirection = direction;

            if (comp.CachedLayerOverrides.TryGetValue(direction, out var layersOverride))
            {
                sprite.LayersOrderOverride = layersOverride;
            }
        }
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

    /// <summary>
    /// Regenerates the directional layer order overrides for an entity.
    /// Must be ran whenever new layers have been added or removed to ensure the indexes point to the correct layers.
    /// </summary>
    /// <param name="entity">The entity to update the overrides of.</param>
    public void RegenerateCachedOverrides(Entity<SpriteDirectionLayeringComponent?> entity)
    {
        if (!_spriteQuery.TryComp(entity.Owner, out var sprite) || entity.Comp == null && !_spriteDirectionQuery.TryComp(entity.Owner, out entity.Comp))
            return;

        foreach (var (direction, list) in entity.Comp.DirectionLayers)
        {
            LinkedList<int>? linkedList;

            if (!entity.Comp.CachedLayerOverrides.TryGetValue(direction, out linkedList))
                linkedList = new LinkedList<int>();
            else
                linkedList.Clear();

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
                        linkedList.AddLast(index);
                    else
                        Log.Warning($"Attempted to add the layer map {enumkey.ToString()} to a layer order override for entity {entity.Owner.ToString()}, but the sprite does not have that layer map!");
                }
                else if (parsedKey is string stringkey)
                {
                    if (_sprite.LayerMapTryGet((entity.Owner, sprite), stringkey, out var index, false))
                        linkedList.AddLast(index);
                    else
                        Log.Warning($"Attempted to add the layer map {stringkey} to a layer order override for entity {entity.Owner.ToString()}, but the sprite does not have that layer map!");
                }
            }

            entity.Comp.CachedLayerOverrides[direction] = linkedList;
        }
    }
}
