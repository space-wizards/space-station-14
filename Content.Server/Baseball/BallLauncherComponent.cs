namespace Content.Server.Baseball
{
    /// <summary>
    /// Component for sports ball launchers, when enabled they launch their inventory of sports balls
    /// </summary>
    [RegisterComponent]
    public sealed class BallLauncherComponent : Component
    {
        // whether the power switch is in "on"
        [ViewVariables] public bool IsOn = false;
        // Whether the power switch is on AND the machine has enough power (so is actively firing)
        [ViewVariables] public bool IsPowered;

        [DataField("shootSpeed")]
        [ViewVariables]
        public float ShootSpeed = 10f;

        [DataField("accumulatedFrametime")]
        public float AccumulatedFrametime;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentLauncherCooldown;
    }
}
