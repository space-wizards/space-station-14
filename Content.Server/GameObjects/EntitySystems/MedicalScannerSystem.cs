using Content.Server.GameObjects.Components.Medical;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class MedicalScannerSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(MedicalScannerComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<MedicalScannerComponent>();
                comp.Update(frameTime);
            }
        }
    }
}
