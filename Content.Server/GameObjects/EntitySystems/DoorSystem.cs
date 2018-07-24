using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace Content.Server.GameObjects.EntitySystems
{
    class DoorSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ServerDoorComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<ServerDoorComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
