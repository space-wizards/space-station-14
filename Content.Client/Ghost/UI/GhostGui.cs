using Content.Client.Stylesheets;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Ghost.UI
{
    public sealed class GhostGui : Control
    {
        private readonly Button _returnToBody = new() {Text = Loc.GetString("ghost-gui-return-to-body-button") };
        private readonly Button _ghostWarp = new() {Text = Loc.GetString("ghost-gui-ghost-warp-button") };
        private readonly Button _ghostRoles = new();
        private readonly Button _ghostToggleGhosts = new() {Text = Loc.GetString("ghost-gui-toggleghosts", ("switch", Loc.GetString("ghost-gui-no"))) };
        private readonly Button _ghostToggleFOV = new() {Text = Loc.GetString("ghost-gui-togglefov", ("switch", Loc.GetString("ghost-gui-yes"))), Pressed = true };
        private readonly Button _ghostToggleShadows = new() {Text = Loc.GetString("ghost-gui-toggleshadows", ("switch", Loc.GetString("ghost-gui-no"))) };
        private readonly Button _ghostToggleLights = new() {Text = Loc.GetString("ghost-gui-togglelight", ("switch", Loc.GetString("ghost-gui-no"))) };

        private readonly GhostComponent _component;
        private readonly GhostSystem _system;

        public GhostTargetWindow? TargetWindow { get; }

        public GhostGui(GhostComponent component, GhostSystem system, IEntityNetworkManager eventBus)
        {
            IoCManager.InjectDependencies(this);

            _component = component;
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
                IoCManager.Resolve<IClientConsoleHost>()
                .RemoteExecuteCommand(null, "ghostroles");
            };

            _ghostToggleGhosts.OnPressed += _ =>
            {
                system.GhostVisibility = !system.GhostVisibility;
                if (system.GhostVisibility == false)
                {
                    _ghostToggleGhosts.Text =
                    Loc.GetString("ghost-gui-toggleghosts", ("switch", Loc.GetString("ghost-gui-yes")));
                }
                if (system.GhostVisibility == true)
                {
                    _ghostToggleGhosts.Text =
                    Loc.GetString("ghost-gui-toggleghosts", ("switch", Loc.GetString("ghost-gui-no")));
                }

                _ghostToggleGhosts.Pressed = !system.GhostVisibility;
            };
            _ghostToggleFOV.OnPressed += _ =>
            {
                var currentEye = IoCManager.Resolve<IEyeManager>().CurrentEye;
                currentEye.DrawFov = !currentEye.DrawFov;
                if (currentEye.DrawFov == false)
                {
                    _ghostToggleFOV.Text =
                    Loc.GetString("ghost-gui-togglefov", ("switch", Loc.GetString("ghost-gui-yes")));
                }
                if (currentEye.DrawFov == true)
                {
                    _ghostToggleFOV.Text =
                    Loc.GetString("ghost-gui-togglefov", ("switch", Loc.GetString("ghost-gui-no")));
                }

                _ghostToggleFOV.Pressed = !currentEye.DrawFov;
            };
            var lightingManager = IoCManager.Resolve<ILightManager>();
            _ghostToggleShadows.OnPressed += _ =>
            {
                lightingManager.DrawShadows = !lightingManager.DrawShadows;
                if (lightingManager.DrawShadows == false)
                {
                    _ghostToggleShadows.Text =
                    Loc.GetString("ghost-gui-toggleshadows", ("switch", Loc.GetString("ghost-gui-yes")));
                }
                if (lightingManager.DrawShadows == true)
                {
                    _ghostToggleShadows.Text =
                    Loc.GetString("ghost-gui-toggleshadows", ("switch", Loc.GetString("ghost-gui-no")));
                }

                _ghostToggleShadows.Pressed = !lightingManager.DrawShadows;
            };
            _ghostToggleLights.OnPressed += _ =>
            {
                lightingManager.Enabled = !lightingManager.Enabled;
                if (lightingManager.Enabled == false)
                {
                    _ghostToggleLights.Text =
                    Loc.GetString("ghost-gui-togglelight", ("switch", Loc.GetString("ghost-gui-yes")));
                }
                if (lightingManager.Enabled == true)
                {
                    _ghostToggleLights.Text =
                    Loc.GetString("ghost-gui-togglelight", ("switch", Loc.GetString("ghost-gui-no")));
                }

                _ghostToggleLights.Pressed = !lightingManager.Enabled;
            };

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children =
                        {
                            _ghostWarp,
                            _ghostRoles,
                        },
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Align = AlignMode.End,
                        Children =
                        {
                            _returnToBody,
                        },
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children =
                        {
                            _ghostToggleGhosts,
                            _ghostToggleFOV,
                        },
                    },
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children =
                        {
                            _ghostToggleShadows,
                            _ghostToggleLights,
                        },
                    }
                }
            });
        }

        public void Update()
        {
            _returnToBody.Disabled = !_component.CanReturnToBody;
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
