using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Research.Prototypes;

namespace Content.Client.Lathe;

public sealed class LatheSystem : SharedLatheSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LatheComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) &&
            args.Sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out _))
        {
            args.Sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
        }

        // Lathe specific stuff
        if (args.Component.TryGetData(LatheVisuals.IsRunning, out bool isRunning))
        {
            var state = isRunning ? component.RunningState : component.IdleState;
            args.Sprite.LayerSetAnimationTime(LatheVisualLayers.IsRunning, 0f);
            args.Sprite.LayerSetState(LatheVisualLayers.IsRunning, state);
        }

        if (args.Component.TryGetData(LatheVisuals.IsInserting, out bool isInserting)
            && args.Sprite.LayerMapTryGet(LatheVisualLayers.IsInserting, out var isInsertingLayer))
        {
            if (args.Component.TryGetData(LatheVisuals.InsertingColor, out Color color)
                && !component.IgnoreColor)
            {
                args.Sprite.LayerSetColor(isInsertingLayer, color);
            }

            args.Sprite.LayerSetAnimationTime(isInsertingLayer, 0f);
            args.Sprite.LayerSetVisible(isInsertingLayer, isInserting);
        }
    }

    ///<remarks>
    /// Whether or not a recipe is available is not really visible to the client,
    /// so it just defaults to true.
    ///</remarks>
    protected override bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component)
    {
        return true;
    }
}

public enum LatheVisualLayers : byte
{
    IsRunning,
    IsInserting
}
