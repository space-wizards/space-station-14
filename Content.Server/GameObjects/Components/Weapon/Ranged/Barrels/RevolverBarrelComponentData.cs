#nullable enable
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class RevolverBarrelComponentData
    {
        [DataClassTarget("ammoSlots")]
        public IEntity[]? AmmoSlots;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "capacity",
                6,
                cap => AmmoSlots = cap != null ? new IEntity[(int)cap] : null,
                () => AmmoSlots?.Length);
        }
    }
}
