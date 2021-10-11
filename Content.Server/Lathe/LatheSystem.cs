using Content.Server.Lathe.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    internal sealed class LatheSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<LatheComponent>(true))
            {
                if (comp.Producing == false && comp.Queue.Count > 0)
                {
                    comp.Produce(comp.Queue.Dequeue());
                }
            }
        }
    }
}
