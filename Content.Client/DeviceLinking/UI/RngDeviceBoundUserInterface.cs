using Content.Client.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Client.DeviceLinking.UI;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Shared.UserInterface;

namespace Content.Client.DeviceLinking.UI;

public sealed class RngDeviceBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RngDeviceWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RngDeviceWindow>();

        _window.OnMuteToggled += OnMuteToggled;
        _window.OnEdgeModeToggled += OnEdgeModeToggled;
        _window.OnTargetNumberChanged += OnTargetNumberChanged;

        _window.OpenCentered();

        // Initial update when opening
        Update();
    }

    private void OnMuteToggled(bool muted)
    {
        SendMessage(new RngDeviceToggleMuteMessage(muted));
    }

    private void OnEdgeModeToggled(bool edgeMode)
    {
        SendMessage(new RngDeviceToggleEdgeModeMessage(edgeMode));
    }

    private void OnTargetNumberChanged(int targetNumber)
    {
        SendMessage(new RngDeviceSetTargetNumberMessage(targetNumber));
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent<RngDeviceComponent>(Owner, out var component))
            return;

        _window.SetMuted(component.Muted);
        _window.SetEdgeMode(component.EdgeMode);
        _window.SetTargetNumber(component.TargetNumber);
        _window.SetTargetNumberVisibility(component.Outputs == 2);
        _window.SetDeviceType(component.StatePrefix);
    }
}
