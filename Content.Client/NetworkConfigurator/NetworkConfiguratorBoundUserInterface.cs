using Content.Shared.DeviceNetwork;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorBoundUserInterface : BoundUserInterface
{
    private NetworkConfiguratorListMenu? _listMenu;
    private NetworkConfiguratorConfigurationMenu? _configurationMenu;

    public NetworkConfiguratorBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    public void OnRemoveButtonPressed(string address)
    {
        SendMessage(new NetworkConfiguratorRemoveDeviceMessage(address));
    }

    protected override void Open()
    {
        base.Open();

        switch (UiKey)
        {
            case NetworkConfiguratorUiKey.List:
                _listMenu = new NetworkConfiguratorListMenu(this);
                _listMenu.OnClose += Close;
                _listMenu.ClearButton.OnPressed += _ => OnClearButtonPressed();
                _listMenu.OpenCentered();
                break;
            case NetworkConfiguratorUiKey.Configure:
                _configurationMenu = new NetworkConfiguratorConfigurationMenu();
                _configurationMenu.OnClose += Close;
                _configurationMenu.Set.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Set);
                _configurationMenu.Add.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Add);
                //_configurationMenu.Edit.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Edit);
                _configurationMenu.Clear.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Clear);
                _configurationMenu.Copy.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Copy);
                _configurationMenu.Show.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Show);
                _configurationMenu.OpenCentered();
                break;
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (NetworkConfiguratorUserInterfaceState) state;
        _listMenu?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _listMenu?.Dispose();
        _configurationMenu?.Dispose();
    }

    private void OnClearButtonPressed()
    {
        SendMessage(new NetworkConfiguratorClearDevicesMessage());
    }

    private void OnConfigButtonPressed(NetworkConfiguratorButtonKey buttonKey)
    {
        SendMessage(new NetworkConfiguratorButtonPressedMessage(buttonKey));
    }
}
