using Content.Server.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems.Body.Surgery
{
    [UsedImplicitly]
    public class SurgeryToolSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var tool in ComponentManager.EntityQuery<SurgeryToolComponent>())
            {
                if (tool.PerformerCache == null)
                {
                    continue;
                }

                if (tool.BodyCache == null)
                {
                    continue;
                }

                if (!ActionBlockerSystem.CanInteract(tool.PerformerCache) ||
                    !tool.PerformerCache.InRangeUnobstructed(tool.BodyCache))
                {
                    tool.CloseAllSurgeryUIs();
                }
            }
        }
    }
}
