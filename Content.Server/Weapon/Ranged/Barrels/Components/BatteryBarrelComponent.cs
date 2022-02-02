using System;
using Content.Server.PowerCell;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent, NetworkedComponent, ComponentProtoName("BatteryBarrel"), ComponentReference(typeof(ServerRangedBarrelComponent))]
    public sealed class BatteryBarrelComponent : ServerRangedBarrelComponent
    {
        // The minimum change we need before we can fire
        [DataField("lowerChargeLimit")]
        [ViewVariables]
        public float LowerChargeLimit = 10;

        [DataField("fireCost")]
        [ViewVariables]
        public int BaseFireCost = 300;

        // What gets fired
        [DataField("ammoPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        [ViewVariables]
        public string? AmmoPrototype;

        public ContainerSlot AmmoContainer = default!;

        public override int ShotsLeft
        {
            get
            {

                if (!EntitySystem.Get<PowerCellSystem>().TryGetBatteryFromSlot(Owner, out var battery))
                {
                    return 0;
                }

                return (int) Math.Ceiling(battery.CurrentCharge / BaseFireCost);
            }
        }

        public override int Capacity
        {
            get
            {
                if (!EntitySystem.Get<PowerCellSystem>().TryGetBatteryFromSlot(Owner, out var battery))
                {
                    return 0;
                }

                return (int) Math.Ceiling(battery.MaxCharge / BaseFireCost);
            }
        }
    }
}
