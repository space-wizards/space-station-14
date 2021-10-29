using Content.Server.Gravity.EntitySystems;
using Content.Shared.Gravity;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Gravity
{
    [RegisterComponent]
    [Friend(typeof(GravityGeneratorSystem))]
    public sealed class GravityGeneratorComponent : SharedGravityGeneratorComponent
    {
        public override string Name => "GravityGenerator";

        // 1% charge per second.
        [ViewVariables(VVAccess.ReadWrite)] [DataField("chargeRate")] public float ChargeRate { get; set; } = 0.01f;
        // The gravity generator has two power values.
        // Idle power is assumed to be the power needed to run the control systems and interface.
        [DataField("idlePower")] public float IdlePowerUse { get; set; }
        // Active power is the power needed to keep the gravity field stable.
        [DataField("activePower")] public float ActivePowerUse { get; set; }
        [DataField("lightRadiusMin")] public float LightRadiusMin { get; set; }
        [DataField("lightRadiusMax")] public float LightRadiusMax { get; set; }


        /// <summary>
        /// Is the power switch on?
        /// </summary>
        [DataField("switchedOn")]
        public bool SwitchedOn { get; set; } = true;

        /// <summary>
        /// Is the gravity generator intact?
        /// </summary>
        [DataField("intact")]
        public bool Intact { get; set; } = true;

        // 0 -> 1
        [ViewVariables(VVAccess.ReadWrite)] [DataField("charge")] public float Charge { get; set; } = 1;

        /// <summary>
        /// Is the gravity generator currently "producing" gravity?
        /// </summary>
        [ViewVariables]
        public bool GravityActive { get; set; }

        /// <summary>
        /// Serialized copy of <see cref="GravityActive"/> used to avoid shaking the grid on first tick.
        /// </summary>
        [DataField("active")] public bool GravityActiveStored { get; set; } = true;

        // Do we need a UI update even if the charge doesn't change? Used by power button.
        [ViewVariables] public bool NeedUIUpdate { get; set; }
    }
}
