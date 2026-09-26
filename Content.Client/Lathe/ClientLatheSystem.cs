using Content.Client.Lathe.UI;
using Content.Client.Power;
using Content.Shared.Lathe;
using Content.Shared.Lathe.Components;
using Content.Shared.Power;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Lathe;

public sealed partial class ClientLatheSystem : LatheSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<LatheVisualsComponent> lathe, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Lathe specific stuff
        if (_appearance.TryGetData<bool>(lathe, LatheVisuals.IsRunning, out var isRunning, args.Component))
        {
            if (_sprite.LayerMapTryGet((lathe, args.Sprite), LatheVisualLayers.IsRunning, out var runningLayer, false) &&
                lathe.Comp.RunningState != null &&
                lathe.Comp.IdleState != null)
            {
                var state = isRunning ? lathe.Comp.RunningState : lathe.Comp.IdleState;
                _sprite.LayerSetRsiState((lathe, args.Sprite), runningLayer, state);
            }
        }

        if (_appearance.TryGetData<bool>(lathe, PowerDeviceVisuals.Powered, out var powered, args.Component) &&
            _sprite.LayerMapTryGet((lathe, args.Sprite), PowerDeviceVisualLayers.Powered, out var powerLayer, false))
        {
            _sprite.LayerSetVisible((lathe, args.Sprite), powerLayer, powered);

            if (lathe.Comp.UnlitIdleState != null &&
                lathe.Comp.UnlitRunningState != null)
            {
                var state = isRunning ? lathe.Comp.UnlitRunningState : lathe.Comp.UnlitIdleState;
                _sprite.LayerSetRsiState((lathe, args.Sprite), powerLayer, state);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<LatheComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current == null)
            return;

        // TODO: EXPLICITLY CALL UPDATE METHODS WHICH AlSO CALL THEIR RESPECTIVE UI UPDATES!!!
        if (args.Current is LatheComponentDeltaState deltaState)
        {
            var changed = deltaState.ChangedFields;
            if ((changed & (1UL << LatheComponentDeltaState.QueueIndex)) != 0)
                entity.Comp.Queue = deltaState.Queue;

            if ((changed & (1UL << LatheComponentDeltaState.CurrentRecipeIndex)) != 0)
                entity.Comp.CurrentRecipe = deltaState.CurrentRecipe;

            if ((changed & (1UL << LatheComponentDeltaState.RecipesIndex)) != 0)
                entity.Comp.Recipes = deltaState.Recipes;

            return;
        }

        // If we're getting a full state then update everything.
        if (args.Current is LatheComponentState state)
        {
            entity.Comp.Recipes = state.Recipes;
            entity.Comp.Queue = state.Queue;
            entity.Comp.CurrentRecipe = state.CurrentRecipe;
            UpdateUI(entity);
        }
    }

    protected override void UpdateUI(Entity<LatheComponent> entity)
    {
        // TODO: Ensure that this is only called when ABSOLUTELY NECESSARY CAUSE IT'S EXPENSIVE AS FUCK ATM
        // TODO: Optimize the fuck out of this.
        Log.Debug($"UI updated at {Timing.CurTime}");
        if (UISys.TryGetOpenUi<LatheBoundUserInterface>(entity.Owner, LatheUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}

public enum LatheVisualLayers : byte
{
    IsRunning
}
