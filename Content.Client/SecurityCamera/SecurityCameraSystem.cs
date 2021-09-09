using Robust.Shared.GameObjects;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.GameObjects;
using Content.Client.Viewport;
using Content.Shared.Movement;
using Content.Shared.SecurityCamera;

namespace Content.Client.SecurityCamera
{
    public class SecurityCameraSystem : EntitySystem
    {
        SS14Window currentWindow = new SS14Window{Title = "Temporary"};
        SecurityClientComponent cameraclient = new SecurityClientComponent();
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<SecurityCameraConnectEvent>(HandleSecurityCameraConnectEvent);
            SubscribeLocalEvent<SecurityClientComponent, MovementAttemptEvent>(HandleMovementEvent);
        }
        private void HandleMovementEvent(EntityUid uid, SecurityClientComponent component, MovementAttemptEvent args)
        {
            if(component.active == true)
            {
                args.Cancel();
            }
        }

        private void HandleSecurityCameraConnectEvent(SecurityCameraConnectEvent msg)
        {
            // Set Vars
            var user = EntityManager.GetEntity(msg.User);
            var eye = EntityManager.GetEntity(msg.Camera).GetComponent<EyeComponent>().Eye;
            var secClient = user.EnsureComponent<SecurityClientComponent>();
            secClient.active = true;
            cameraclient = secClient;
            if(eye != null)
            {
                // Create Viewing Point
                var viewport = new ScalingViewport
                {
                    Eye = eye,
                    ViewportSize = (512, 512),
                    RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt
                };
                if(currentWindow.Title == "Temporary")
                {
                    // Create New Window
                    var window = new SS14Window
                    {
                        MinWidth = 512,
                        MinHeight = 512,
                        Title = "Cameras"
                    };

                    window.Contents.AddChild(viewport);
                    window.OnClose += () => HandleSecurityDisconnectEvent();

                    currentWindow = window;
                    currentWindow.OpenCentered();
                    return;
                }
                if(currentWindow.Title == "Cameras")
                {
                    // Cycle Viewpoints
                    currentWindow.Contents.Children.Clear();
                    currentWindow.Contents.AddChild(viewport);
                }
            }
        }

        private void HandleSecurityDisconnectEvent()
        {
            RaiseNetworkEvent(new SecurityCameraDisconnectEvent(cameraclient.Owner.Uid));
            cameraclient.active = false;
            currentWindow.Title = "Temporary";
        }
    }
}