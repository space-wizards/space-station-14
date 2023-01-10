using Content.Shared.Gravity;

namespace Content.Server.Gravity
{
    [RegisterComponent]
    [Access(typeof(GravityGeneratorSystem))]
    public sealed class GravityGeneratorComponent : SharedGravityGeneratorComponent
    {
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
        public bool GravityActive { get; set; } = false;

        // Do we need a UI update even if the charge doesn't change? Used by power button.
        [ViewVariables] public bool NeedUIUpdate { get; set; }
    }
}
