using Content.Shared.Light;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.Client.PDA;

public sealed class PDAVisualizerSystem : VisualizerSystem<PDAVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PDAVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, PDAVisualizerComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (comp.State != null)
        {
            sprite.LayerMapSet(PDAVisualLayers.Base, sprite.AddLayerState(comp.State));
        }

        sprite.LayerMapSet(PDAVisualLayers.Flashlight, sprite.AddLayerState("light_overlay"));
        sprite.LayerSetShader(PDAVisualLayers.Flashlight, "unshaded");
        sprite.LayerMapSet(PDAVisualLayers.IDLight, sprite.AddLayerState("id_overlay"));
        sprite.LayerSetShader(PDAVisualLayers.IDLight, "unshaded");

        if (TryComp<PDAComponent>(uid, out var pda))
            sprite.LayerSetVisible(PDAVisualLayers.IDLight, pda.IdSlot.StartingItem != null);
    }

    protected override void OnAppearanceChange(EntityUid uid, PDAVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        args.Sprite.LayerSetVisible(PDAVisualLayers.Flashlight, false);
        if (AppearanceSystem.TryGetData(uid, UnpoweredFlashlightVisuals.LightOn, out bool isFlashlightOn, args.Component))
        {
            args.Sprite.LayerSetVisible(PDAVisualLayers.Flashlight, isFlashlightOn);
        }
        if (AppearanceSystem.TryGetData(uid, PDAVisuals.IDCardInserted, out bool isCardInserted, args.Component))
        {
            args.Sprite.LayerSetVisible(PDAVisualLayers.IDLight, isCardInserted);
        }
    }
}

enum PDAVisualLayers : byte
{
    Base,
    Flashlight,
    IDLight
}
