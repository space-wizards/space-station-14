using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Lathe.Components;

namespace Content.Client.Lathe;

public sealed class LatheSystem : SharedLatheSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
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

    private void OnAfterAutoHandleState(Entity<LatheComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        UpdateUI(entity);
    }

    protected override void UpdateUI(Entity<LatheComponent> entity)
    {
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
