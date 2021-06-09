#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SolutionAreaEffectSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var inception in ComponentManager.EntityQuery<SolutionAreaEffectInceptionComponent>().ToArray())
            {
                inception.InceptionUpdate(frameTime);
            }
        }
    }
}
