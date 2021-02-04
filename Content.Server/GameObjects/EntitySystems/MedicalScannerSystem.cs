using Content.Server.GameObjects.Components.Medical;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class MedicalScannerSystem : EntitySystem
    {

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<MedicalScannerComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
