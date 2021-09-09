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
            SubscribeLocalEvent<SecurityClientComponent, MovementAttemptEvent>(HandleMovementEvent);
            SubscribeNetworkEvent<SecurityCameraDisconnectEvent>(HandleSecurityCameraDisconnectEvent);
        }

        private void HandleMovementEvent(EntityUid uid, SecurityClientComponent component, MovementAttemptEvent args)
        {
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

        public void HandleInteract(EntityUid uid, SecurityConsoleComponent component, InteractHandEvent args)
        {
            var user = args.User;
            UpdateList(user);
            Logger.Info("Updated List");
            var secClient = user.EnsureComponent<SecurityClientComponent>();
            RaiseNetworkEvent(new SecurityCameraConnectEvent(user.Uid,cameraList),user.GetComponent<ActorComponent>().PlayerSession.ConnectedClient);
            Logger.Info("Sent Event");
            secClient.active = true;               
        }

        public void UpdateList(IEntity user)
        {
            // Update List Of Cameras
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