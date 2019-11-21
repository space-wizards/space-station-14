using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    class FuelTankComponent : Component
    {
        public override string Name => "FuelTank";

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }
    }
}
