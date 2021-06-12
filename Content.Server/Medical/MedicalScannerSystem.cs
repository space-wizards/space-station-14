using Content.Server.Medical.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Medical
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
