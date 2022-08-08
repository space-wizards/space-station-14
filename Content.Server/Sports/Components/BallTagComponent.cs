namespace Content.Server.Sports.Components
{
    /// <summary>
    /// This component is added to baseballs to let them tag people when used on them
    /// Popups a "Urist McHands has tagged Urist McCaldwell with the baseball!"
    /// </summary>
    [RegisterComponent]
    public sealed class BallTagComponent : Component
    {

        public TimeSpan LastUseTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 5f;
    }
}
