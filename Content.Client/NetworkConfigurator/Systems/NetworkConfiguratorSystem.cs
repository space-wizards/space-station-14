using Content.Client.Actions;
using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.NetworkConfigurator.Systems;

public sealed class NetworkConfiguratorSystem : SharedNetworkConfiguratorSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string Action = "ActionClearNetworkLinkOverlays";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClearAllOverlaysEvent>(_ => ClearAllOverlays());
        SubscribeLocalEvent<NetworkConfiguratorComponent, ItemStatusCollectMessage>(OnCollectItemStatus);
    }

    private void OnCollectItemStatus(EntityUid uid, NetworkConfiguratorComponent configurator, ItemStatusCollectMessage args)
    {
        _inputManager.TryGetKeyBinding((ContentKeyFunctions.AltUseItemInHand), out var binding);
        args.Controls.Add(new StatusControl(configurator, binding?.GetKeyString() ?? ""));
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
            RemComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
            if (!_overlay.TryGetOverlay(out NetworkConfiguratorLinkOverlay? overlay))
                return;

            overlay.Colors.Remove(component.ActiveDeviceList.Value);
            if (overlay.Colors.Count > 0)
                return;

            _actions.RemoveAction(overlay.Action);
            _overlay.RemoveOverlay<NetworkConfiguratorLinkOverlay>();
            return;
        }

        if (!_overlay.HasOverlay<NetworkConfiguratorLinkOverlay>())
        {
            var overlay = new NetworkConfiguratorLinkOverlay();
            _overlay.AddOverlay(overlay);
            var player = _playerManager.LocalPlayer.ControlledEntity.Value;
            overlay.Action = Spawn(Action);
            _actions.AddActionDirect(player, overlay.Action.Value);
        }

        EnsureComp<NetworkConfiguratorActiveLinkOverlayComponent>(component.ActiveDeviceList.Value);
    }

    public void ClearAllOverlays()
    {
        if (!_overlay.TryGetOverlay(out NetworkConfiguratorLinkOverlay? overlay))
        {
            return;
        }

        var query = EntityQueryEnumerator<NetworkConfiguratorActiveLinkOverlayComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<NetworkConfiguratorActiveLinkOverlayComponent>(uid);
        }

        _actions.RemoveAction(overlay.Action);
        _overlay.RemoveOverlay(overlay);
    }

    private sealed class StatusControl : Control
    {
        private readonly RichTextLabel _label;
        private readonly NetworkConfiguratorComponent _configurator;
        private readonly string _keyBindingName;

        private bool? _linkModeActive = null;

        public StatusControl(NetworkConfiguratorComponent configurator, string keyBindingName)
        {
            _configurator = configurator;
            _keyBindingName = keyBindingName;
            _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_linkModeActive != null && _linkModeActive == _configurator.LinkModeActive)
                return;

            _linkModeActive = _configurator.LinkModeActive;

            var modeLocString = _linkModeActive??false
                ? "network-configurator-examine-mode-link"
                : "network-configurator-examine-mode-list";

            _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("network-configurator-item-status-label",
                ("mode", Robust.Shared.Localization.Loc.GetString(modeLocString)),
                ("keybinding", _keyBindingName)));
        }
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
