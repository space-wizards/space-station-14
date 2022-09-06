using System.Linq;
using Content.Client.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.DeviceNetwork;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.NetworkConfigurator;

public sealed class NetworkConfiguratorSystem : SharedNetworkConfiguratorSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    private const string Action = "ClearNetworkLinkOverlays";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClearAllOverlaysEvent>(_ => ClearAllOverlays());
    }

    public bool ConfiguredListIsTracked(EntityUid uid, NetworkConfiguratorComponent? component = null)
    {
        return Resolve(uid, ref component)
               && component.ActiveDeviceList != null
               && HasComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
    }

    /// <summary>
    /// Toggles a device list's (tied to this network configurator) connection visualisation on and off.
    /// </summary>
    public void ToggleVisualization(EntityUid uid, bool toggle, NetworkConfiguratorComponent? component = null)
    {
        if (_playerManager.LocalPlayer == null
            || _playerManager.LocalPlayer.ControlledEntity == null
            || !Resolve(uid, ref component)
            || component.ActiveDeviceList == null)
            return;

        if (!toggle)
        {
            if (_overlay.HasOverlay<NetworkConfiguratorLinkOverlay>())
            {
                _overlay.GetOverlay<NetworkConfiguratorLinkOverlay>().ClearEntity(component.ActiveDeviceList.Value);
            }

            RemComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
            if (!EntityQuery<NetworkConfiguratorActiveLinkOverlayComponent>().Any())
            {
                _overlay.RemoveOverlay<NetworkConfiguratorLinkOverlay>();
                _actions.RemoveAction(_playerManager.LocalPlayer.ControlledEntity.Value, _prototypeManager.Index<InstantActionPrototype>(Action));
            }


            return;
        }

        if (!_overlay.HasOverlay<NetworkConfiguratorLinkOverlay>())
        {
            _overlay.AddOverlay(new NetworkConfiguratorLinkOverlay());
            _actions.AddAction(_playerManager.LocalPlayer.ControlledEntity.Value, new InstantAction(_prototypeManager.Index<InstantActionPrototype>(Action)), null);
        }

        EnsureComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
    }

    public void ClearAllOverlays()
    {
        if (!_overlay.HasOverlay<NetworkConfiguratorLinkOverlay>())
        {
            return;
        }

        foreach (var tracker in EntityQuery<NetworkConfiguratorActiveLinkOverlayComponent>())
        {
            RemCompDeferred<NetworkConfiguratorActiveLinkOverlayComponent>(tracker.Owner);
        }

        _overlay.RemoveOverlay<NetworkConfiguratorLinkOverlay>();

        if (_playerManager.LocalPlayer?.ControlledEntity != null)
        {
            _actions.RemoveAction(_playerManager.LocalPlayer.ControlledEntity.Value, _prototypeManager.Index<InstantActionPrototype>(Action));
        }
    }

    // hacky solution related to mapping
    public void SetActiveDeviceList(EntityUid tool, EntityUid list, NetworkConfiguratorComponent? component = null)
    {
        if (!Resolve(tool, ref component))
        {
            return;
        }

        component.ActiveDeviceList = list;
    }
}

public sealed class ClearAllNetworkLinkOverlays : IConsoleCommand
{
    public string Command => "clearnetworklinkoverlays";
    public string Description => "Clear all network link overlays.";
    public string Help => Command;
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        IoCManager.Resolve<IEntityManager>().System<NetworkConfiguratorSystem>().ClearAllOverlays();
    }
}
