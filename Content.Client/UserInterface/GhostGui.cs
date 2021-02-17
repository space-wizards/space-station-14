#nullable enable
using Content.Client.GameObjects.Components.Observer;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Vector2 = Robust.Shared.Maths.Vector2;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface
{
    public class GhostGui : Control
    {
        private readonly Button _returnToBody = new() {Text = Loc.GetString("Return to body")};
        private readonly Button _ghostWarp = new() {Text = Loc.GetString("Ghost Warp")};
        private readonly Button _ghostRoles = new() {Text = Loc.GetString("Ghost Roles")};
        private readonly GhostComponent _owner;

        public GhostTargetWindow? TargetWindow { get; private set; }

        public GhostGui(GhostComponent owner)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;

            TargetWindow = new GhostTargetWindow(owner);

            MouseFilter = MouseFilterMode.Ignore;

            _ghostWarp.OnPressed += args => TargetWindow.Populate();
            _returnToBody.OnPressed += args => owner.SendReturnToBodyMessage();
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
        protected override Vector2? CustomSize => (300, 450);
        private readonly GhostComponent _owner;
        private readonly VBoxContainer _buttonContainer;

        public GhostTargetWindow(GhostComponent owner)
        {
            Title = "Ghost Warp";
            _owner = owner;
            _owner.GhostRequestWarpPoint();
            _owner.GhostRequestPlayerNames();

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };

            _buttonContainer = new VBoxContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.Fill,
                SeparationOverride = 5,

            };

            var scrollBarContainer = new ScrollContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };

            margin.AddChild(scrollBarContainer);
            scrollBarContainer.AddChild(_buttonContainer);

            Contents.AddChild(margin);
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
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    SizeFlagsStretchRatio = 1,
                    CustomMinimumSize = (230, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (args) =>
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
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    SizeFlagsStretchRatio = 1,
                    CustomMinimumSize = (230,20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (args) =>
                {
                    _owner.SendGhostWarpRequestMessage(default,name);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }
    }
}
