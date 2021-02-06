using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class PumpBarrelComponentData
    {
        [DataClassTarget("capacity")]
        public int Capacity = 6;


        [DataClassTarget("spawnedAmmo")]
        public Stack<IEntity> SpawnedAmmo;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref Capacity, "capacity", 6);
            SpawnedAmmo = new Stack<IEntity>(Capacity - 1);
        }
    }
}
