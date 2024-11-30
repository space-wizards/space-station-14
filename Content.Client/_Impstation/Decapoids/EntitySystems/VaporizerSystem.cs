using Content.Client._Impstation.Decapoids.Components;
using Content.Shared._Impstation.Decapoids;
using Robust.Client.GameObjects;

namespace Content.Client._Impstation.Decapoids.EntitySystems;

public sealed partial class VaporizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VaporizerComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(EntityUid uid, VaporizerComponent comp, AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(VaporizerVisuals.VisualState, out var state))
            return;

        if (args.Sprite == null)
            return;

        var layer = args.Sprite.LayerMapReserveBlank(VaporizerVisualLayers.Indicator);

        switch (state)
        {
            case VaporizerState.Normal:
                args.Sprite.LayerSetState(layer, "normal");
                break;
            case VaporizerState.BadSolution:
                args.Sprite.LayerSetState(layer, "bad");
                break;
            case VaporizerState.LowSolution:
                args.Sprite.LayerSetState(layer, "low");
                break;
            case VaporizerState.Empty:
                args.Sprite.LayerSetState(layer, "empty");
                break;
        }
    }
}
