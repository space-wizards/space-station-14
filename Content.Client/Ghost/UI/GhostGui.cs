using Content.Client.Stylesheets;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Ghost.UI
{
    public class GhostGui : Control
    {
        private readonly Button _returnToBody = new() {Text = Loc.GetString("ghost-gui-return-to-body-button") };
        private readonly Button _ghostWarp = new() {Text = Loc.GetString("ghost-gui-ghost-warp-button") };
        private readonly Button _ghostRoles = new();
        private readonly GhostComponent _owner;
        private readonly GhostSystem _system;

        public GhostTargetWindow? TargetWindow { get; }

        public GhostGui(GhostComponent owner, GhostSystem system, IEntityNetworkManager eventBus)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;
            _system = system;

            TargetWindow = new GhostTargetWindow(eventBus);

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

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
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
            _ghostRoles.Text = Loc.GetString("ghost-gui-ghost-roles-button", ("count", _system.AvailableGhostRoleCount));
            if (_system.AvailableGhostRoleCount != 0)
            {
                _ghostRoles.StyleClasses.Add(StyleBase.ButtonCaution);
            }
            else
            {
                _ghostRoles.StyleClasses.Remove(StyleBase.ButtonCaution);
            }
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
}
