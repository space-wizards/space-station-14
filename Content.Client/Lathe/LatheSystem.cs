using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Research.Prototypes;

namespace Content.Client.Lathe;

public sealed class LatheSystem : SharedLatheSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LatheComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, PowerDeviceVisuals.Powered, out var powered, args.Component) &&
            args.Sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out _))
        {
            args.Sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
        }

        // Lathe specific stuff
        if (_appearance.TryGetData<bool>(uid, LatheVisuals.IsRunning, out var isRunning, args.Component))
        {
            var state = isRunning ? component.RunningState : component.IdleState;
            args.Sprite.LayerSetAnimationTime(LatheVisualLayers.IsRunning, 0f);
            args.Sprite.LayerSetState(LatheVisualLayers.IsRunning, state);
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
    IsRunning
}
