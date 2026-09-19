using Content.Shared.Conveyor;
using Content.Shared.Materials;
using Robust.Client.GameObjects;

namespace Content.Client.Materials;

public sealed partial class RecyclerVisualizerSystem : VisualizerSystem<RecyclerVisualsComponent>
{
    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, RecyclerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !SpriteSystem.LayerMapTryGet((uid, args.Sprite), RecyclerVisualLayers.Main, out var layer, false))
            return;

        args.TryGetData<ConveyorState>(ConveyorVisuals.State, out var running);
        args.TryGetData<bool>(RecyclerVisuals.Bloody, out var bloody);
        args.TryGetData<bool>(RecyclerVisuals.Broken, out var broken);

        var activityState = running == ConveyorState.Off ? 0 : 1;
        if (broken) //breakage overrides activity
            activityState = 2;

        var bloodyKey = bloody ? component.BloodyKey : string.Empty;

        var state = $"{component.BaseKey}{activityState}{bloodyKey}";
        SpriteSystem.LayerSetRsiState((uid, args.Sprite), layer, state);
    }
}
