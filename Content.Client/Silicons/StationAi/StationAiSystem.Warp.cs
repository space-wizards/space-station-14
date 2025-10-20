using System;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private StationAiWarpWindow? _warpWindow;

    private void InitializeWarp()
    {
        SubscribeLocalEvent<StationAiHeldComponent, StationAiOpenWarpAction>(OnOpenWarpAction);
        SubscribeNetworkEvent<StationAiWarpTargetsEvent>(OnWarpTargets);
    }

    private void ShutdownWarp()
    {
        if (_warpWindow != null)
        {
            _warpWindow.TargetSelected -= OnWarpTargetSelected;
            _warpWindow.OnClose -= OnWarpWindowClosed;
            _warpWindow.Close();
            _warpWindow = null;
        }

    }

    private void OnOpenWarpAction(Entity<StationAiHeldComponent> ent, ref StationAiOpenWarpAction args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        EnsureWarpWindow();
        _warpWindow?.SetLoading(true);
        RaiseNetworkEvent(new StationAiWarpRequestEvent());
    }

    private void OnWarpTargets(StationAiWarpTargetsEvent msg, EntitySessionEventArgs args)
    {
        if (_player.LocalEntity is not { } local || !HasComp<StationAiHeldComponent>(local))
            return;

        EnsureWarpWindow();
        _warpWindow?.SetTargets(msg.Targets);
    }

    private void OnWarpTargetSelected(StationAiWarpTarget target)
    {
        RaiseNetworkEvent(new StationAiWarpToTargetEvent(target.Target));
        _warpWindow?.Close();
    }

    private void OnWarpWindowClosed()
    {
        if (_warpWindow == null)
            return;

        _warpWindow.TargetSelected -= OnWarpTargetSelected;
        _warpWindow.OnClose -= OnWarpWindowClosed;
        _warpWindow = null;
    }

    private void EnsureWarpWindow()
    {
        if (_warpWindow != null)
        {
            if (!_warpWindow.IsOpen)
                _warpWindow.OpenCentered();
            return;
        }

        _warpWindow = new StationAiWarpWindow();
        _warpWindow.TargetSelected += OnWarpTargetSelected;
        _warpWindow.OnClose += OnWarpWindowClosed;
        _warpWindow.OpenCentered();
    }
}
