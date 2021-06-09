#nullable enable
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.Ghost
{
    public class GhostGui : Control
    {
        private readonly Button _returnToBody = new() {Text = Loc.GetString("Return to body")};
        private readonly Button _ghostWarp = new() {Text = Loc.GetString("Ghost Warp")};
        private readonly Button _ghostRoles = new() {Text = Loc.GetString("Ghost Roles")};
        private readonly GhostComponent _owner;

        public GhostTargetWindow? TargetWindow { get; }

        public GhostGui(GhostComponent owner)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;

            TargetWindow = new GhostTargetWindow(owner);

            MouseFilter = MouseFilterMode.Ignore;

            _ghostWarp.OnPressed += _ => TargetWindow.Populate();
            _returnToBody.OnPressed += _ => owner.SendReturnToBodyMessage();
            _ghostRoles.OnPressed += _ => IoCManager.Resolve<IClientConsoleHost>().RemoteExecuteCommand(null, "ghostroles");

            AddChild(new HBoxContainer
            {
                Children =
                {
                    _returnToBody,
                    _ghostWarp,
                    _ghostRoles,
                }
            });

            Update();
        }

        public void Update()
        {
            _returnToBody.Disabled = !_owner.CanReturnToBody;
        }
    }

    public class GhostTargetWindow : SS14Window
    {
        private readonly GhostComponent _owner;
        private readonly VBoxContainer _buttonContainer;

        public GhostTargetWindow(GhostComponent owner)
        {
            MinSize = SetSize = (300, 450);
            Title = "Ghost Warp";
            _owner = owner;
            _owner.GhostRequestWarpPoint();
            _owner.GhostRequestPlayerNames();

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
            OpenCentered();
        }

        private void AddButtonPlayers()
        {
            foreach (var (key, value) in _owner.PlayerNames)
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
                    _owner.SendGhostWarpRequestMessage(key);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }

        private void AddButtonLocations()
        {
            foreach (var name in _owner.WarpNames)
            {
                var currentButtonRef = new Button
                {
                    Text = $"Warp: {name}",
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230,20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (_) =>
                {
                    _owner.SendGhostWarpRequestMessage(name);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }
    }
}
