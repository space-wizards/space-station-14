using Content.Client.NetworkConfigurator.Systems;
using Content.Shared.DeviceNetwork;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorBoundUserInterface : BoundUserInterface
{
    private readonly NetworkConfiguratorSystem _netConfig;

    [ViewVariables]
    private NetworkConfiguratorConfigurationMenu? _configurationMenu;

    [ViewVariables]
    private NetworkConfiguratorLinkMenu? _linkMenu;

    [ViewVariables]
    private NetworkConfiguratorListMenu? _listMenu;

    public NetworkConfiguratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _netConfig = EntMan.System<NetworkConfiguratorSystem>();
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
                _listMenu = this.CreateWindow<NetworkConfiguratorListMenu>();
                _listMenu.ClearButton.OnPressed += _ => OnClearButtonPressed();
                _listMenu.OnRemoveAddress += OnRemoveButtonPressed;
                break;
            case NetworkConfiguratorUiKey.Configure:
                _configurationMenu = this.CreateWindow<NetworkConfiguratorConfigurationMenu>();
                _configurationMenu.Set.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Set);
                _configurationMenu.Add.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Add);
                //_configurationMenu.Edit.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Edit);
                _configurationMenu.Clear.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Clear);
                _configurationMenu.Copy.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Copy);
                _configurationMenu.Show.OnPressed += OnShowPressed;
                _configurationMenu.Show.Pressed = _netConfig.ConfiguredListIsTracked(Owner);
                _configurationMenu.OnRemoveAddress += OnRemoveButtonPressed;
                break;
            case NetworkConfiguratorUiKey.Link:
                _linkMenu = this.CreateWindow<NetworkConfiguratorLinkMenu>();
                _linkMenu.OnLinkDefaults += args =>
                {
                    SendMessage(new NetworkConfiguratorLinksSaveMessage(args));
                };

                _linkMenu.OnToggleLink += (left, right) =>
                {
                    SendMessage(new NetworkConfiguratorToggleLinkMessage(left, right));
                };

                _linkMenu.OnClearLinks += () =>
                {
                    SendMessage(new NetworkConfiguratorClearLinksMessage());
                };
                break;
        }
    }

    private void OnShowPressed(BaseButton.ButtonEventArgs args)
    {
        _netConfig.ToggleVisualization(Owner, args.Button.Pressed);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case NetworkConfiguratorUserInterfaceState configState:
                _listMenu?.UpdateState(configState);
                break;
            case DeviceListUserInterfaceState listState:
                _configurationMenu?.UpdateState(listState);
                break;
            case DeviceLinkUserInterfaceState linkState:
                _linkMenu?.UpdateState(linkState);
                break;
        }
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
