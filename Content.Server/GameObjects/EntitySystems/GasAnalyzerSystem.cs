using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class GasAnalyzerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var analyzer in ComponentManager.EntityQuery<GasAnalyzerComponent>())
            {
                analyzer.Update(frameTime);
            }
        }
    }
}
