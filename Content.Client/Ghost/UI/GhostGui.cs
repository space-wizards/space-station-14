using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Ghost.UI
{
    public sealed class GhostGui : Control
    {
        private readonly Button _returnToBody = new() {Text = Loc.GetString("ghost-gui-return-to-body-button") };
        private readonly Button _ghostWarp = new() {Text = Loc.GetString("ghost-gui-ghost-warp-button") };
        private readonly Button _ghostRoles = new();
        private readonly GhostComponent _owner;
        private readonly GhostSystem _system;
        private readonly HashSet<string> _lastSeenGhostRoles = new();

        public GhostTargetWindow? TargetWindow { get; }

        public GhostGui(GhostComponent owner, GhostSystem system, IEntityNetworkManager eventBus, bool isAdmin)
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
            _ghostRoles.OnPressed += _ =>
            {
                _lastSeenGhostRoles.Clear();
                _lastSeenGhostRoles.UnionWith(_system.AvailableGhostRoles);
                _ghostRoles.StyleClasses.Remove(StyleBase.ButtonCaution);

                IoCManager.Resolve<IClientConsoleHost>()
                    .RemoteExecuteCommand(null, "ghostroles");
            };

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

            // Colour the button if there are any new roles since the button was last opened.
            _lastSeenGhostRoles.IntersectWith(_system.AvailableGhostRoles);
            if (_system.AvailableGhostRoles.Any(role => !_lastSeenGhostRoles.Contains(role)))
                _ghostRoles.StyleClasses.Add(StyleBase.ButtonCaution);
            else
                _ghostRoles.StyleClasses.Remove(StyleBase.ButtonCaution);

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
