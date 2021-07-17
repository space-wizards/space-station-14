using System.Collections.Generic;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Ghost
{
    public class GhostGui : Control
    {
        private readonly Button _returnToBody = new() {Text = Loc.GetString("ghost-gui-return-to-body-button") };
        private readonly Button _ghostWarp = new() {Text = Loc.GetString("ghost-gui-ghost-warp-button") };
        private readonly Button _ghostRoles = new() {Text = Loc.GetString("ghost-gui-ghost-roles-button") };
        private readonly GhostComponent _owner;

        public GhostTargetWindow? TargetWindow { get; }

        public GhostGui(GhostComponent owner, IEntityNetworkManager eventBus)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;

            TargetWindow = new GhostTargetWindow(owner, eventBus);

            MouseFilter = MouseFilterMode.Ignore;

            _ghostWarp.OnPressed += _ =>
            {
                eventBus.SendSystemNetworkMessage(new GhostWarpsRequestEvent());
                TargetWindow.Populate();
                TargetWindow.OpenCentered();
            };
            _returnToBody.OnPressed += _ =>
            {
                var msg = new GhostReturnToBodyRequest();
                eventBus.SendSystemNetworkMessage(msg);
            };
            _ghostRoles.OnPressed += _ => IoCManager.Resolve<IClientConsoleHost>()
                .RemoteExecuteCommand(null, "ghostroles");

            AddChild(new HBoxContainer
            {
                Children =
                {
                    _returnToBody,
                    _ghostWarp,
                    _ghostRoles,
                }
            });
        }

        public void Update()
        {
            _returnToBody.Disabled = !_owner.CanReturnToBody;
            TargetWindow?.Populate();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                TargetWindow?.Dispose();
            }
        }
    }

    public class GhostTargetWindow : SS14Window
    {
        private readonly GhostComponent _owner;
        private readonly IEntityNetworkManager _netManager;

        private readonly VBoxContainer _buttonContainer;

        public List<string> Locations { get; set; } = new();

        public Dictionary<EntityUid, string> Players { get; set; } = new();

        public GhostTargetWindow(GhostComponent owner, IEntityNetworkManager netManager)
        {
            MinSize = SetSize = (300, 450);
            Title = Loc.GetString("ghost-target-window-title");
            _owner = owner;
            _netManager = netManager;

            _buttonContainer = new VBoxContainer()
            {
                VerticalExpand = true,
                SeparationOverride = 5,

            };

            var scrollBarContainer = new ScrollContainer()
            {
                VerticalExpand = true,
                HorizontalExpand = true
            };

            scrollBarContainer.AddChild(_buttonContainer);

            Contents.AddChild(scrollBarContainer);
        }

        public void Populate()
        {
            _buttonContainer.DisposeAllChildren();
            AddButtonPlayers();
            AddButtonLocations();
        }

        private void AddButtonPlayers()
        {
            foreach (var (key, value) in Players)
            {
                var currentButtonRef = new Button
                {
                    Text = value,
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (_) =>
                {
                    var msg = new GhostWarpToTargetRequestEvent(key);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }

        private void AddButtonLocations()
        {
            foreach (var name in Locations)
            {
                var currentButtonRef = new Button
                {
                    Text = Loc.GetString("ghost-target-window-current-button", ("name", name)),
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230,20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (_) =>
                {
                    var msg = new GhostWarpToLocationRequestEvent(name);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }
    }
}
