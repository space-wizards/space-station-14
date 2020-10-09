using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class ExtinguisherCabinetFilledComponent : ExtinguisherCabinetComponent
    {
        public override string Name => "ExtinguisherCabinetFilled";

        public override void Initialize()
        {
            base.Initialize();

            ItemContainer.Insert(Owner.EntityManager.SpawnEntity("FireExtinguisher", Owner.Transform.Coordinates));
        }
    }
}
