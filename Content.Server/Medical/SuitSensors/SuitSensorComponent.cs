using Content.Shared.Inventory;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Medical.SuitSensors
{
    /// <summary>
    ///     Tracking device, embedded in almost all uniforms and jumpsuits.
    ///     If enabled, will report to crew monitoring console owners position and status.
    /// </summary>
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
        /// <summary>
        /// Sensor doesn't send any information about owner
        /// </summary>
        SensorOff = 0,

        /// <summary>
        /// Sensor sends only binary status (alive/dead)
        /// </summary>
        SensorBinary = 1,

        /// <summary>
        /// Sensor sends health vitals status
        /// </summary>
        SensorVitals = 2,

        /// <summary>
        /// Sensor sends vitals status and GPS position
        /// </summary>
        SensorCords = 3
    }
}
