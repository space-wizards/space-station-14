using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client.Storage.Visualizers;

public sealed class BagOpenCloseVisualizerSystem : VisualizerSystem<BagOpenCloseVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BagOpenCloseVisualizerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BagOpenCloseVisualizerComponent, ComponentStartup>(OnStartup);
    }

    private void OnInit(EntityUid uid, BagOpenCloseVisualizerComponent comp, ComponentInit args)
    {
        if(comp.OpenIcon == null){
            Logger.Warning("BagOpenCloseVisualizer is useless with no `openIcon`");
        }
    }

    private void OnStartup(EntityUid uid, BagOpenCloseVisualizerComponent comp, ComponentStartup args)
    {
        if (comp.OpenIcon != null &&
            TryComp<SpriteComponent>(uid, out var spriteComponent) &&
            spriteComponent.BaseRSI?.Path is { } path)
        {
            spriteComponent.LayerMapReserveBlank(BagOpenCloseVisualizerComponent.OpenIconLayer);
            spriteComponent.LayerSetSprite(BagOpenCloseVisualizerComponent.OpenIconLayer, new Rsi(path, comp.OpenIcon));
            spriteComponent.LayerSetVisible(BagOpenCloseVisualizerComponent.OpenIconLayer, false);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, BagOpenCloseVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (comp.OpenIcon == null)
            return;
        if (args.Sprite == null)
            return;
        if(!AppearanceSystem.TryGetData<SharedBagState>(uid, SharedBagOpenVisuals.BagState, out var bagState, args.Component))
            return;

        switch (bagState)
        {
            case SharedBagState.Open:
                args.Sprite.LayerSetVisible(BagOpenCloseVisualizerComponent.OpenIconLayer, true);
                break;
            default:
                args.Sprite.LayerSetVisible(BagOpenCloseVisualizerComponent.OpenIconLayer, false);
                break;
        }
    }
}
