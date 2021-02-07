using Content.Server.GameObjects.Components.Research;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class LatheSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<LatheComponent>(true))
            {
                if (comp.Producing == false && comp.Queue.Count > 0)
                {
                    comp.Produce(comp.Queue.Dequeue());
                }
            }
        }
    }
}
