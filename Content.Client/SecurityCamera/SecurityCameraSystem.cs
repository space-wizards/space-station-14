using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Localization;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.GameObjects;
using Content.Client.Camera;
using Content.Client.Viewport;
using Content.Shared.Movement;
using Content.Shared.SecurityCamera;
using System.Collections.Generic;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Robust.Shared.Log;
namespace Content.Client.SecurityCamera
{
    public class SecurityCameraSystem : EntitySystem
    {
        SS14Window currentWindow = new SS14Window{Title = "Temporary"};
        SecurityClientComponent cameraclient = new SecurityClientComponent();
        public Dictionary<int,EntityUid> cameraList = new();

        private Button nextButton = new Button{Text = Loc.GetString("security-camera-next-button")};
        private Button prevButton = new Button{Text = Loc.GetString("security-camera-prev-button")};
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<SecurityCameraConnectEvent>(HandleSecurityCameraConnectEvent);
            SubscribeNetworkEvent<PowerChangedDisconnectEvent>(HandlePowerChangedDisconnectEvent);
            SubscribeLocalEvent<SecurityClientComponent, MovementAttemptEvent>(HandleMovementEvent);
        }
        private void HandleMovementEvent(EntityUid uid, SecurityClientComponent component, MovementAttemptEvent args)
        {
            // Cancel movement
            if(component.active == true)
            {
                args.Cancel();
            }
        }

        private void HandlePowerChangedDisconnectEvent(PowerChangedDisconnectEvent msg)
        {
            // Handle power changes
            if(msg.Console == cameraclient.connectedComputer){HandleSecurityDisconnectEvent();}
        }

        private void HandleSecurityCameraConnectEvent(SecurityCameraConnectEvent msg)
        {
            // Set variables
            cameraList = msg.CameraList;
            var user = EntityManager.GetEntity(msg.User);
            var secClient = user.EnsureComponent<SecurityClientComponent>();
            secClient.active = true;
            secClient.connectedComputer = msg.Console;
            secClient.currentCamInt = 1;
            cameraclient = secClient;
            var eye = EntityManager.GetEntity(cameraList[1]).GetComponent<EyeComponent>();
            // Create new viewport
            var viewport = EntitySystem.Get<CameraSystem>().CreateCameraViewport(new Vector2i(512,512),eye,ScalingViewportRenderScaleMode.CeilInt);
            if(currentWindow.Title == "Temporary")
            {       
                // Create new window
                currentWindow = new SS14Window
                {
                    Title = "Cameras",
                    MinSize = (500, 600)
                };
                // Create Ui
                currentWindow.Contents.AddChild(viewport);
                currentWindow.Contents.AddChild(new BoxContainer
                {
	                Orientation = LayoutOrientation.Vertical,
                    Children =  {
                        new BoxContainer
                        {
                            Orientation = LayoutOrientation.Horizontal,
                            Children =
                                {
                                    prevButton,
                                    nextButton
                                }
                }}});
                nextButton.OnPressed += OnNextButtonPressed;
                prevButton.OnPressed += OnPrevButtonPressed;
                currentWindow.OnClose += () => HandleSecurityDisconnectEvent();
                currentWindow.OpenToLeft();
                return;
            }
            if(currentWindow.Title == "Cameras")
            {
                // Reopen stored window
                currentWindow.Open();
            }
        }
        
        private void OnNextButtonPressed(ButtonEventArgs args)
        {
            int nextCam = cameraclient.currentCamInt + 1;
            if(nextCam > cameraList.Count) nextCam = 1;
            cameraclient.currentCamInt = nextCam;
            ChangeCam(nextCam);
        }

        private void OnPrevButtonPressed(ButtonEventArgs args)
        {
            int nextCam = cameraclient.currentCamInt - 1;
            if(nextCam < 1) nextCam = cameraList.Count;
            cameraclient.currentCamInt = nextCam;
            ChangeCam(nextCam);
        }

        private void ChangeCam(int camera)
        {
            // Cycle cameras
            var eye = EntityManager.GetEntity(cameraList[camera]).GetComponent<EyeComponent>().Eye;
            foreach(var child in currentWindow.Contents.Children)
            {
                if(child is ScalingViewport viewport)
                {
                    viewport.Eye = eye;
                }
            }
        }

        private void HandleSecurityDisconnectEvent()
        {
            RaiseNetworkEvent(new SecurityCameraDisconnectEvent(cameraclient.Owner.Uid));
            currentWindow.Close();
            cameraclient.active = false;
        }
    }
}