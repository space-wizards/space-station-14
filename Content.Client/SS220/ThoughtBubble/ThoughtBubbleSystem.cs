// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using System.Numerics;
using Content.Shared.SS220.ThoughtBubble;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.SS220.ThoughtBubble;

/// <summary>
/// Handles thought bubble visuals - spawns, updates position/rotation
/// </summary>
public sealed class ThoughtBubbleSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float ItemSpriteScale = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThoughtBubbleComponent, AfterAutoHandleStateEvent>(OnStateHandled);
        SubscribeLocalEvent<ThoughtBubbleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStateHandled(Entity<ThoughtBubbleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (GetEntity(ent.Comp.PointedItem) is not { Valid: true } item)
            return;

        if (ent.Comp.ShownInBubbleItem == item)
            return;

        ent.Comp.ShownInBubbleItem = item;

        PredictedQueueDel(ent.Comp.BubbleEntity);
        if (!TryComp<SpriteComponent>(item, out var itemSprite))
            return;

        var thought = SpawnAttachedTo(ent.Comp.BubbleProto, new EntityCoordinates(ent.Owner, Vector2.Zero));
        ent.Comp.BubbleEntity = thought;

        if (!TryComp<SpriteComponent>(thought, out var thoughtSprite))
            return;

        if (TryComp<SpriteComponent>(ent.Owner, out var ownerSprite) && ownerSprite.DrawDepth > thoughtSprite.DrawDepth)
            // +1 to draw over owner
            _sprite.SetDrawDepth((thought, thoughtSprite), ownerSprite.DrawDepth + 1);

        var layerIndex = 1;
        foreach (var layer in itemSprite.AllLayers)
        {
            if (layer is not SpriteComponent.Layer spriteLayer)
                continue;

            var protoData = spriteLayer.ToPrototypeData();
            protoData.RsiPath ??= spriteLayer.ActualRsi?.Path.CanonPath;
            protoData.Scale *= ItemSpriteScale;

            _sprite.AddLayer((thought, thoughtSprite), protoData, layerIndex++);
        }
    }

    private void OnShutdown(Entity<ThoughtBubbleComponent> ent, ref ComponentShutdown args)
    {
        PredictedQueueDel(ent.Comp.BubbleEntity);
        ent.Comp.BubbleEntity = null;
    }
}
