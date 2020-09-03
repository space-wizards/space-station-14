using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class ExtinguisherCabinetFilledComponent : ExtinguisherCabinetComponent
    {
        public override string Name => "ExtinguisherCabinetFilled";

        public override void Initialize()
        {
            base.Initialize();

            ItemContainer.Insert(Owner.EntityManager.SpawnEntity("FireExtinguisher", Owner.Transform.GridPosition));
        }
    }
}
