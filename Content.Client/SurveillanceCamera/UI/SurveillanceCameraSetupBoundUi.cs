using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed class SurveillanceCameraSetupBoundUi : BoundUserInterface
{
    private SurveillanceCameraSetupWindow? _window;
    private SurveillanceCameraSetupUiKey _type;

    public SurveillanceCameraSetupBoundUi(ClientUserInterfaceComponent component, Enum uiKey) : base(component, uiKey)
    {
        if (uiKey is not SurveillanceCameraSetupUiKey key)
        {
            return;
        }

        _type = key;
    }

    protected override void Open()
    {
        _window = new();

        if (_type == SurveillanceCameraSetupUiKey.Router)
        {
            _window.HideNameSelector();
        }

        _window.OpenCentered();
        _window.OnNameConfirm += SendDeviceName;
        _window.OnNetworkConfirm += SendSelectedNetwork;

    }

    private void SendSelectedNetwork(int idx)
    {
        SendMessage(new SurveillanceCameraSetupSetNetwork(idx));
    }

    private void SendDeviceName(string name)
    {
        SendMessage(new SurveillanceCameraSetupSetName(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SurveillanceCameraSetupBoundUiState cast)
        {
            return;
        }

        _window.UpdateState(cast.Name, cast.NameDisabled, cast.NetworkDisabled);
        _window.LoadAvailableNetworks(cast.Network, cast.Networks);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window!.Dispose();
        }
    }
}
