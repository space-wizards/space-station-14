using System.Linq;
using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class SolutionAreaEffectSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var inception in EntityManager.EntityQuery<SolutionAreaEffectInceptionComponent>().ToArray())
            {
                inception.InceptionUpdate(frameTime);
            }
        }
    }
}
