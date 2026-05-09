using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Lathe.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Lathe;

public sealed partial class LatheSystem : SharedLatheSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<LatheComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, LatheComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Lathe specific stuff
        if (_appearance.TryGetData<bool>(uid, LatheVisuals.IsRunning, out var isRunning, args.Component))
        {
            if (_sprite.LayerMapTryGet((uid, args.Sprite), LatheVisualLayers.IsRunning, out var runningLayer, false) &&
                component.RunningState != null &&
                component.IdleState != null)
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                _sprite.LayerSetRsiState((uid, args.Sprite), runningLayer, state);
            }
        }

        if (_appearance.TryGetData<bool>(uid, PowerDeviceVisuals.Powered, out var powered, args.Component) &&
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

    private void OnHandleState(Entity<LatheComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not LatheComponentState state)
            return;

        entity.Comp.Recipes = state.Recipes;
        entity.Comp.Queue = state.Queue;
        entity.Comp.CurrentRecipe = state.Recipe;
        UpdateUI(entity);
    }

    protected override void UpdateUI(Entity<LatheComponent> entity)
    {
        // TODO: This is an extremely CPU intensive process and isn't predicting properly. Debug WHY.
        Log.Debug($"UI updated at {Timing.CurTime}");
        if (UISys.TryGetOpenUi(entity.Owner, LatheUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}

public enum LatheVisualLayers : byte
{
    IsRunning
}
