using Content.Shared.Inventory;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Medical.SuitSensors
{
    [RegisterComponent]
    [Friend(typeof(SuitSensorSystem))]
    public class SuitSensorComponent : Component
    {
        public override string Name => "SuitSensor";

        /// <summary>
        ///     Choose a random sensor mode when item is spawned.
        /// </summary>
        [DataField("randomMode")]
        public bool RandomMode = true;

        /// <summary>
        ///     Current sensor mode. Can be switched by user verbs.
        /// </summary>
        [DataField("mode")]
        public SuitSensorMode Mode = SuitSensorMode.SensorOff;

        /// <summary>
        ///     Activate sensor if user wear it in this slot.
        /// </summary>
        [DataField("activationSlot")]
        public EquipmentSlotDefines.Slots ActivationSlot = EquipmentSlotDefines.Slots.INNERCLOTHING;

        /// <summary>
        ///     Current user that wears suit sensor. Null if nobody wearing it.
        /// </summary>
        public EntityUid? User = null;
    }

    public enum SuitSensorMode : byte
    {
        SensorOff = 0,
        SensorBinary = 1,
        SensorVitals = 2,
        SensorCords = 3
    }
}
