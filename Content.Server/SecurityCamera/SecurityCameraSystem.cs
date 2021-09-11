using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Server.GameObjects;
using System.Collections.Generic;
using Content.Shared.Movement;
using Content.Shared.SecurityCamera;
using Content.Shared.Interaction;
using Content.Server.Camera;
using Content.Server.Power.Components;
using Robust.Shared.Log;

namespace Content.Server.SecurityCamera
{
    public class SecurityCameraSystem : EntitySystem
    {
        public Dictionary<int,EntityUid> cameraList = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SecurityConsoleComponent,InteractHandEvent>(HandleInteract);
            SubscribeLocalEvent<SecurityConsoleComponent, PowerChangedEvent>(HandlePowerChangedEvent);
            SubscribeLocalEvent<SecurityCameraComponent, SecurityCameraConnectionChangedEvent>(HandleSecurityCameraConnectionChangedEvent);
            SubscribeLocalEvent<SecurityClientComponent, MovementAttemptEvent>(HandleMovementEvent);
            SubscribeNetworkEvent<SecurityCameraDisconnectEvent>(HandleSecurityCameraDisconnectEvent);
        }

        private void HandleMovementEvent(EntityUid uid, SecurityClientComponent component, MovementAttemptEvent args)
        {
            // Cancel movement
            if(component.active == true)
            {
                args.Cancel();
            }
        }

        private void HandleSecurityCameraDisconnectEvent(SecurityCameraDisconnectEvent msg)
        {
            var user = EntityManager.GetEntity(msg.User);
            var secClient = user.GetComponent<SecurityClientComponent>();
            if(secClient != null)
            {
                secClient.active = false;
            }
        }
        private void HandleSecurityCameraConnectionChangedEvent(EntityUid uid,SecurityCameraComponent component,SecurityCameraConnectionChangedEvent msg)
        {
            RaiseNetworkEvent(new SecurityCameraConnectionChangedEvent(msg.Uid,msg.Connected));
            Logger.Info("Received and relayed Info");
        }

        public void HandleInteract(EntityUid uid, SecurityConsoleComponent component, InteractHandEvent args)
        {
            // Check power
            component._powerReceiverComponent = component.Owner.GetComponent<ApcPowerReceiverComponent>();
            if(!component.Powered){return;}

            // Set variables
            var user = args.User;
            UpdateList(user);
            var secClient = user.EnsureComponent<SecurityClientComponent>();
            secClient.connectedComputer = component.Owner.Uid;
            secClient.active = true;   
            RaiseNetworkEvent(new SecurityCameraConnectEvent(user.Uid,cameraList,component.Owner.Uid),user.GetComponent<ActorComponent>().PlayerSession.ConnectedClient);
        
            // Sync connection values
            foreach(var cam in ComponentManager.EntityQuery<SecurityCameraComponent>())
            {
                RaiseNetworkEvent(new SecurityCameraConnectionChangedEvent(cam.Owner.Uid,cam.Owner.GetComponent<SecurityCameraComponent>().Connected),user.GetComponent<ActorComponent>().PlayerSession.ConnectedClient);
            }
        }

        public void HandlePowerChangedEvent(EntityUid uid, SecurityConsoleComponent component, PowerChangedEvent args)
        {
            // Handle power changes
            if(args.Powered){return;}
            RaiseNetworkEvent(new PowerChangedDisconnectEvent(component.Owner.Uid));
        }

        public void UpdateList(IEntity user)
        {
            // Update list of cameras
            foreach(var cam in EntityManager.ComponentManager.EntityQuery<SecurityCameraComponent>(true))
            {
                if(!cameraList.ContainsValue(cam.Owner.Uid))
                {
                    EntitySystem.Get<CameraSystem>().CreateCamera(user,cam.Owner.Transform.MapPosition,true,new Vector2(1,1));
                    cameraList.Add(cameraList.Count + 1, cam.Owner.Uid);
                }
            }
        }
    }
}