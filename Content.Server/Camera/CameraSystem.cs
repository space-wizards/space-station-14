using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Server.GameObjects;

namespace Content.Server.Camera
{
    public class CameraSystem : EntitySystem
    {
        public IEntity CreateCamera(IEntity user,MapCoordinates mapcoordinates, bool drawFov, Vector2 zoom)
        {
            // Create New Entity With EyeComponent
            var camera = EntityManager.SpawnEntity(null,mapcoordinates);

            var eyecomp = camera.EnsureComponent<EyeComponent>();
            eyecomp.DrawFov = drawFov;
            
            EntitySystem.Get<ViewSubscriberSystem>().AddViewSubscriber(camera.Uid, user.GetComponent<ActorComponent>().PlayerSession);

            return camera;
        }
    }
}