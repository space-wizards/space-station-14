using Content.Server.GameObjects.Components.Interactable.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.ToggleSystem
{
    class ToggleSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ToggleComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<ToggleComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
