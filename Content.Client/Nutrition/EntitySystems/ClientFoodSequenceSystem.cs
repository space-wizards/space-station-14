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
        if (!TryComp<SpriteComponent>(start, out var sprite))
            return;

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

            var layer = 0;
            bool anotherLayer = true;
            while(anotherLayer)     //Nested loop to handle multi-layer sprites.
            {
                var keyCode = $"food-layer-{counter}-{layer}";      /* H:nest a loop for each layer in each stored sprite in the List? */
                start.Comp.RevealedLayers.Add(keyCode);
                /* H: TryGetLayer(int, out Layer) could work for pulling layers */
                //state.Sprite.TryGetLayer(layer, out var thisLayer);         this needs to be uncommented if AddLayer(Layer, int) gets publiced for me PLEASE I BEG YOU
                sprite.LayerMapTryGet(start.Comp.TargetLayerMap, out var index);
                //sprite.AddLayer(thisLayer, index);        DOESN'T WORK because the AddLayer I'm trying to use is private.

                if (start.Comp.InverseLayers)       /* H:think this is used primarily for skewers. it all goes in circles. */
                    index++;                        /* H:I do kinda have to figure out how to bypass it for them, though... probably just index = index++ - layer would work */

                sprite.AddBlankLayer(index);
                sprite.LayerMapSet(keyCode, index);
                sprite.LayerSetSprite(index, state.Sprite);
                sprite.LayerSetScale(index, state.Scale);

                //Offset the layer
                var layerPos = start.Comp.StartPosition;
                layerPos += (start.Comp.Offset * counter) + state.LocalOffset;
                sprite.LayerSetOffset(index, layerPos);
                //Checks if another sprite layer in the current sprite layer exists.
                //state.Sprite.LayerExists(++layer, out anotherLayer);
                anotherLayer = false;
            }
            counter++;
        }
    }
}
