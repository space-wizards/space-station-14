using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Localization;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Content.Client.Camera;
using Content.Client.Resources;
using Content.Client.Viewport;
using Content.Shared.Movement;
using Content.Shared.SecurityCamera;
using System.Collections.Generic;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Robust.Client.ResourceManagement;
using Robust.Client.Player;
using Robust.Shared.Log;
using Robust.Shared.IoC;

namespace Content.Client.SecurityCamera
{
    public class SecurityCameraSystem : EntitySystem
    {
        //Saved variables
        private SS14Window currentWindow = new SS14Window{Title = "Temporary"};
        private SecurityClientComponent cameraclient = new SecurityClientComponent();
        private ScalingViewport cameraViewport = new ScalingViewport();
        private TextureRect noSignal = new TextureRect();
        private Dictionary<int,EntityUid> cameraList = new();
        private IResourceCache resourceCache = IoCManager.Resolve<IResourceCache>();
        private EntityUid localPlayer;

        private Button nextButton = new Button{Text = Loc.GetString("security-camera-next-button")};
        private Button prevButton = new Button{Text = Loc.GetString("security-camera-prev-button")};
        
        public override void Initialize()
        {
            base.Initialize();
            
            SubscribeNetworkEvent<SecurityCameraConnectEvent>(HandleSecurityCameraConnectEvent);
            SubscribeNetworkEvent<PowerChangedDisconnectEvent>(HandlePowerChangedDisconnectEvent);
            SubscribeNetworkEvent<SecurityCameraConnectionChangedEvent>(HandleConnectionChangedEvent);
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
        private void HandleConnectionChangedEvent(SecurityCameraConnectionChangedEvent msg)
        {
            //Update connection
            UpdateList();
            if(!cameraList.ContainsValue(msg.Uid))
            {
                cameraList.Add(cameraList.Count + 1,msg.Uid);
                EntityManager.GetEntity(msg.Uid).EnsureComponent<ClientSecurityCameraComponent>();
            }
            EntityManager.GetEntity(msg.Uid).GetComponent<ClientSecurityCameraComponent>().Connected = msg.Connected;
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
            UpdateList();
            var user = EntityManager.GetEntity(msg.User);
            localPlayer = msg.User;
            var secClient = user.EnsureComponent<SecurityClientComponent>();
            secClient.active = true;
            secClient.connectedComputer = msg.Console;
            secClient.currentCamInt = 1;
            cameraclient = secClient;        

            // Create new viewport
            var eye = EntityManager.GetEntity(cameraList[1]).GetComponent<EyeComponent>();
            var viewport = EntitySystem.Get<CameraSystem>().CreateCameraViewport(new Vector2i(450,450),eye,ScalingViewportRenderScaleMode.CeilInt);
    	    
            if(currentWindow.Title == "Temporary")
            {       
                // Create new window
                currentWindow = new SS14Window
                {
                    Title = "Cameras",
                    MinSize = (450, 600)
                };
                // Create Ui
                ChangeCam(1);
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
            UpdateList();
            int nextCam = cameraclient.currentCamInt + 1;
            if(nextCam > cameraList.Count) nextCam = 1;
            cameraclient.currentCamInt = nextCam;
            ChangeCam(cameraclient.currentCamInt);
        }

        private void OnPrevButtonPressed(ButtonEventArgs args)
        {
            UpdateList();
            int nextCam = cameraclient.currentCamInt - 1;
            if(nextCam < 1) nextCam = cameraList.Count;
            cameraclient.currentCamInt = nextCam;
            ChangeCam(cameraclient.currentCamInt);
        }

        private void ChangeCam(int camera)
        {
            // Cycle cameras
            var _resourceCache = IoCManager.Resolve<IResourceCache>();
            var nextcam = EntityManager.GetEntity(cameraList[camera]);
            var eye = nextcam.GetComponent<EyeComponent>();
            cameraViewport = EntitySystem.Get<CameraSystem>().CreateCameraViewport(new Vector2i(450,450),eye,ScalingViewportRenderScaleMode.CeilInt);
            noSignal = new TextureRect
                {
                    Texture = _resourceCache.GetTexture("/Textures/Interface/Misc/no_signal.rsi/no_signal.png"),
                    MinSize = (450, 450),
                    Stretch = TextureRect.StretchMode.Scale,
                    Visible = true,
                    SetSize = (450, 450)
                };
            Logger.Info("Testing connection");
            Logger.Info(nextcam.GetComponent<ClientSecurityCameraComponent>().Connected.ToString());
            if(nextcam.GetComponent<ClientSecurityCameraComponent>().Connected == false)
            {
                Logger.Info("Not connected");
                currentWindow.Contents.Children.Remove(cameraViewport);
                currentWindow.AddChild(noSignal);
                return;
            }
            else
            {
                currentWindow.Contents.Children.Remove(noSignal);
                currentWindow.AddChild(cameraViewport);
                Logger.Info("Connected");
                return;
            }
        }

        private void UpdateList()
        {
            //Ensure ClientSecurityCameraComponents
            foreach(var cam in cameraList.Values)
            {
                var comp = EntityManager.GetEntity(cam).EnsureComponent<ClientSecurityCameraComponent>();
            }
        }

        private void HandleSecurityDisconnectEvent()
        {
            //Send disconnect info to server
            RaiseNetworkEvent(new SecurityCameraDisconnectEvent(cameraclient.Owner.Uid));
            currentWindow.Close();
            cameraclient.active = false;
        }
    }
}