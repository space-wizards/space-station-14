namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Self-recharging battery.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BatterySelfRechargerComponent : Component
    {
        [DataField]
        public bool AutoRecharge { get; set; }

        [DataField]
        public float AutoRechargeRate { get; set; }

        [DataField]
        public bool AutoRechargePause { get; set; } = false;

        [DataField]
        public float AutoRechargePauseTime { get; set; } = 0f;

        [DataField]
        public float AutoRechargeCooldown { get; set; } = 0f;
    }
}
