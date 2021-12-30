using System;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;
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
    [ComponentProtoName("SuitSensor")]
    public sealed class SuitSensorComponent : Component
    {
        /// <summary>
        ///     Choose a random sensor mode when item is spawned.
        /// </summary>
        [DataField("randomMode")]
        public bool RandomMode = true;

        /// <summary>
        ///     If true user can't change suit sensor mode
        /// </summary>
        [DataField("controlsLocked")]
        public bool ControlsLocked = false;

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
        ///     How often does sensor update its owners status (in seconds).
        /// </summary>
        [DataField("updateRate")]
        public float UpdateRate = 2f;

        /// <summary>
        ///     Current user that wears suit sensor. Null if nobody wearing it.
        /// </summary>
        public EntityUid? User = null;

        /// <summary>
        ///     Last time when sensor updated owners status
        /// </summary>
        public TimeSpan LastUpdate = TimeSpan.Zero;
    }
}
