using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class ServerMagazineBarrelComponentData
    {
        [DataClassTarget("types")]
        private MagazineType _magazineTypes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "magazineTypes",
                new List<MagazineType>(),
                types => types.ForEach(mag => _magazineTypes |= mag), () =>
                {
                    var types = new List<MagazineType>();

                    foreach (MagazineType mag in Enum.GetValues(typeof(MagazineType)))
                    {
                        if ((_magazineTypes & mag) != 0)
                        {
                            types.Add(mag);
                        }
                    }

                    return types;
                });
        }
    }
}
