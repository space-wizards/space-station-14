using System;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Self-recharging battery.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BatterySelfRechargerComponent : Component
    {
        /// <summary>
        /// Does the entity auto recharge?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRecharge")] public bool AutoRecharge { get; set; }

        /// <summary>
        /// At what rate does the entity automatically recharge?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargeRate")] public float AutoRechargeRate { get; set; }

        /// <summary>
        /// Should this entity stop automatically recharging if a charge is used?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargePause")] public bool AutoRechargePause { get; set; } = false;

        /// <summary>
        /// How long should the entity stop automatically recharging if a charge is used?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargePauseTime")] public float AutoRechargePauseTime { get; set; } = 0f;

        /// <summary>
        /// Do not auto recharge if this timestamp has yet to happen, set for the auto recharge pause system.
        /// </summary>
        [DataField] public TimeSpan NextAutoRecharge { get; set; } = TimeSpan.FromSeconds(0f);
    }
}
