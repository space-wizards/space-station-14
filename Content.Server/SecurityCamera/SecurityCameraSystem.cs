using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.IoC;
using Robust.Server.GameObjects;
using System.Collections.Generic;
using Content.Shared.Movement;
using Content.Shared.SecurityCamera;
using Content.Shared.Interaction;

namespace Content.Server.SecurityCamera
{
    public class SecurityCameraSystem : EntitySystem
    {
        [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
        public Dictionary<int,SecurityCameraComponent> cameraList = new();

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
            UpdateList();
            var user = args.User;
            var secClient = user.EnsureComponent<SecurityClientComponent>();
            
            // Decide Next Camera
            int nextCam = secClient.currentCamInt + 1;
            if(nextCam > cameraList.Count) nextCam = 1;
            secClient.currentCamInt = nextCam;
            if(cameraList.TryGetValue(nextCam,out SecurityCameraComponent _))
            {
                var camera = CreateCamera(user,cameraList[nextCam].Owner.Transform.MapPosition);
                RaiseNetworkEvent(new SecurityCameraConnectEvent(user.Uid,camera.Uid),user.GetComponent<ActorComponent>().PlayerSession.ConnectedClient);
                secClient.active = true;               
            }
        }

        public void UpdateList()
        {
            // Update List Of Cameras
            foreach(var cam in EntityManager.ComponentManager.EntityQuery<SecurityCameraComponent>(true))
            {
                if(!cameraList.ContainsValue(cam))
                {
                    cameraList.Add(cameraList.Count + 1, cam);
                }
            }
        }

        public IEntity CreateCamera(IEntity user,MapCoordinates coords)
        {
            // Create New Entity With EyeComponent
            var camera = EntityManager.SpawnEntity(null,coords);

            var eyecomp = camera.EnsureComponent<EyeComponent>();
            
            _viewSubscriberSystem.AddViewSubscriber(camera.Uid, user.GetComponent<ActorComponent>().PlayerSession);

            return camera;
        }
    }
}