using Content.Shared.DeviceConfigurator;
using Content.Shared.DeviceConfigurator.Components;
using Content.Shared.DeviceNetwork;
using Robust.Client.UserInterface;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorListBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private NetworkConfiguratorListMenu? _listMenu;

    protected override void Open()
    {
        base.Open();

        _listMenu = this.CreateWindow<NetworkConfiguratorListMenu>();
        _listMenu.ClearButton.OnPressed += _ => OnClearButtonPressed();
        _listMenu.OnRemoveAddress += OnRemoveButtonPressed;
        Update();
    }

    public override void Update()
    {
        base.Update();

        if (!EntMan.TryGetComponent(Owner, out NetworkConfiguratorComponent? config))
            return;

        _listMenu?.UpdateState(config.NamedDevices);
    }

    private void OnClearButtonPressed()
    {
        SendPredictedMessage(new NetworkConfiguratorListClearDevicesMessage());
    }

    private void OnRemoveButtonPressed(DeviceAddress address)
    {
        SendPredictedMessage(new NetworkConfiguratorRemoveDeviceMessage(address));
    }
}
