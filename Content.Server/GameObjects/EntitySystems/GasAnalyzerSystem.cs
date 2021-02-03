using Content.Server.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class GasAnalyzerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var analyzer in ComponentManager.EntityQuery<GasAnalyzerComponent>(true))
            {
                analyzer.Update(frameTime);
            }
        }
    }
}
