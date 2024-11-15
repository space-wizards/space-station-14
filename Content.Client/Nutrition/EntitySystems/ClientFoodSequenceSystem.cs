using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.EntitySystems;

public sealed class ClientFoodSequenceSystem : SharedFoodSequenceSystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FoodSequenceStartPointComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<FoodSequenceStartPointComponent> start, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(start, out var sprite))       /* H:really curious how something that *doesn't have* a SpriteComponent can output a SpriteComponent */
            return;                                                 /* H:after a brief look at both SpriteSpecifier and SpriteComponent... this shouldn't be possible. What am I missing? */

        UpdateFoodVisuals(start, sprite);
    }

    private void UpdateFoodVisuals(Entity<FoodSequenceStartPointComponent> start, SpriteComponent? sprite = null)
    {
        if (!Resolve(start, ref sprite, false))
            return;

        //Remove old layers
        foreach (var key in start.Comp.RevealedLayers)
        {
            sprite.RemoveLayer(key);                    /* H:shouldn't need to change this */
        }
        start.Comp.RevealedLayers.Clear();

        //Add new layers
        var counter = 0;
        foreach (var state in start.Comp.FoodLayers)    /* H:objective: make this compliant with SpriteComponents instead of SpriteSpecifiers */
        {
            if (state.Sprite is null)
                continue;

            var keyCode = $"food-layer-{counter}";      /* H:nest a loop for each layer in each stored sprite in the List? */
            start.Comp.RevealedLayers.Add(keyCode);

            sprite.LayerMapTryGet(start.Comp.TargetLayerMap, out var index);        /* H:this part could be a smol issue */

            if (start.Comp.InverseLayers)       /* H:think this is used primarily for skewers. it all goes in circles. */
                index++;

            sprite.AddBlankLayer(index);
            sprite.LayerMapSet(keyCode, index);
            sprite.LayerSetSprite(index, state.Sprite);
            sprite.LayerSetScale(index, state.Scale);

            //Offset the layer                             /* H:stuff below has to be excluded from nested loop */
            var layerPos = start.Comp.StartPosition;
            layerPos += (start.Comp.Offset * counter) + state.LocalOffset;
            sprite.LayerSetOffset(index, layerPos);

            counter++;
        }
    }
}
