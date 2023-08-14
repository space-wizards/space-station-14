using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Mech.Equipment.Components;
public sealed class MechGunComponent : Component
{
    [DataField("fireEnergyDelta")]
    public float FireEnergyDelta = -20;

    [DataField("gunPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GunPrototype = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container GunContainer = default!;
}
