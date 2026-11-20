using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Research.Prototypes;

namespace Content.Client.Lathe;

public sealed partial class LatheSystem : SharedLatheSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LatheComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Lathe specific stuff
        if (args.TryGetData<bool>(LatheVisuals.IsRunning, out var isRunning))
        {
            if (_sprite.LayerMapTryGet((uid, args.Sprite), LatheVisualLayers.IsRunning, out var runningLayer, false) &&
                component.RunningState != null &&
                component.IdleState != null)
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                _sprite.LayerSetRsiState((uid, args.Sprite), runningLayer, state);
            }
        }

        if (args.TryGetData<bool>(PowerDeviceVisuals.Powered, out var powered) &&
            _sprite.LayerMapTryGet((uid, args.Sprite), PowerDeviceVisualLayers.Powered, out var powerLayer, false))
        {
            _sprite.LayerSetVisible((uid, args.Sprite), powerLayer, powered);

            if (component.UnlitIdleState != null &&
                component.UnlitRunningState != null)
            {
                var state = isRunning ? component.UnlitRunningState : component.UnlitIdleState;
                _sprite.LayerSetRsiState((uid, args.Sprite), powerLayer, state);
            }
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
