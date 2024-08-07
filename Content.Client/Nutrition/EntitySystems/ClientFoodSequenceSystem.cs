using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Nutrition.EntitySystems;

public sealed class ClientFoodSequenceSystem : SharedFoodSequenceSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<FoodSequenceStartPointComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<FoodSequenceStartPointComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<List<FoodSequenceElementEntry>>(ent, FoodSequenceVisuals.Layers, out var layers, args.Component))
            return;

        UpdateFoodVisuals(ent, layers);
    }

    private void UpdateFoodVisuals(Entity<FoodSequenceStartPointComponent> start, List<FoodSequenceElementEntry> layers, SpriteComponent? sprite = null)
    {
        if (!Resolve(start, ref sprite, false))
            return;

        //Remove old layers
        foreach (var key in start.Comp.RevealedLayers)
        {
            sprite.RemoveLayer(key);
        }
        start.Comp.RevealedLayers.Clear();

        //Add new layers
        var counter = 0;
        foreach (var state in layers)
        {
            counter++;

            var keyCode = $"food-layer-{counter}";
            start.Comp.RevealedLayers.Add(keyCode);

            //Set image
            var index = sprite.LayerMapReserveBlank(keyCode);
            sprite.LayerSetRSI(index, start.Comp.RsiPath);
            sprite.LayerSetState(index, state.State);

            //Offset the layer
            var LayerPos = start.Comp.StartPosition;
            LayerPos += start.Comp.Offset * counter;
            sprite.LayerSetOffset(index, LayerPos);
        }
    }
}
