using Content.Client.NetworkConfigurator.Systems;
using Content.Shared.DeviceConfigurator;
using Content.Shared.DeviceConfigurator.Components;
using Content.Shared.DeviceNetwork;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorBoundUserInterface : BoundUserInterface
{
    private readonly NetworkConfiguratorOverlaySystem _netConfigOverlay;

    [ViewVariables]
    private NetworkConfiguratorConfigurationMenu? _configurationMenu;

    public NetworkConfiguratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _netConfigOverlay = EntMan.System<NetworkConfiguratorOverlaySystem>();
    }

    public void OnRemoveButtonPressed(LocDeviceAddress address)
    {
        SendPredictedMessage(new NetworkConfiguratorRemoveDeviceMessage(address));
    }

    protected override void Open()
    {
        base.Open();

        _configurationMenu = this.CreateWindow<NetworkConfiguratorConfigurationMenu>();
        _configurationMenu.Set.OnPressed += _ => SendPredictedMessage(new NetworkConfiguratorSetMessage());
        _configurationMenu.Add.OnPressed += _ => SendPredictedMessage(new NetworkConfiguratorAddMessage());
        //_configurationMenu.Edit.OnPressed += _ => OnConfigButtonPressed(NetworkConfiguratorButtonKey.Edit);
        _configurationMenu.Clear.OnPressed += _ => SendPredictedMessage(new NetworkConfiguratorClearMessage());
        _configurationMenu.Copy.OnPressed += _ => SendPredictedMessage(new NetworkConfiguratorCopyMessage());
        _configurationMenu.Show.OnPressed += OnShowPressed;
        _configurationMenu.Show.Pressed = _netConfigOverlay.ConfiguredListIsTracked(Owner);
        _configurationMenu.OnRemoveAddress += OnRemoveButtonPressed;
        Update();
    }

    private void OnShowPressed(BaseButton.ButtonEventArgs args)
    {
        _netConfigOverlay.ToggleVisualization(Owner, args.Button.Pressed);
    }

    public override void Update()
    {
        base.Update();

        if (!EntMan.TryGetComponent(Owner, out NetworkConfiguratorComponent? config))
            return;

        _configurationMenu?.UpdateState(config.NamedDevices);
    }
}
