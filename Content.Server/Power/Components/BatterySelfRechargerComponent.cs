namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Self-recharging battery.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BatterySelfRechargerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRecharge")] public bool AutoRecharge { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargeRate")] public float AutoRechargeRate { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargePause")] public bool AutoRechargePause { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargePauseTime")] public float AutoRechargePauseTime { get; set; } = 0f;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRechargeCooldown")] public float AutoRechargeCooldown { get; set; } = 0f;
    }
}
