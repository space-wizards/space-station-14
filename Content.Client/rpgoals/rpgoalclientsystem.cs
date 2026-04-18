using Content.Shared.RPGoals;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Client.RPGoals;

[UsedImplicitly]
public sealed class RPGoalClientSystem : EntitySystem
{
    private RPGoalSelectionWindow? _window;
    private bool _requestedForCurrentAttach;

    public override void Initialize()
    {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<RPGoalSelectionState>(OnSelectionState);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (_requestedForCurrentAttach)
            return;

        _requestedForCurrentAttach = true;
        RaiseNetworkEvent(new RPGoalSelectionRequest());
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _requestedForCurrentAttach = false;
        _window?.Close();
    }

    private void OnSelectionState(RPGoalSelectionState state)
    {
        _window ??= new RPGoalSelectionWindow();
        _window.UpdateState(state);
        _window.OnAcceptPressed = goalId => RaiseNetworkEvent(new RPGoalAcceptMessage(goalId));
        _window.OnRerollPressed = () => RaiseNetworkEvent(new RPGoalRerollMessage());

        if (state.Finalized)
        {
            _window.Close();
            return;
        }

        if (!_window.IsOpen)
            _window.OpenCentered();
    }
}
