namespace Content.Shared.Zombies
{
    [RegisterComponent]
    public sealed class InitialInfectedComponent : Component
    {
        /// <summary>
        /// A time after which this initial infected player can turn.
        /// </summary>
        [DataField("firstTurnAllowed"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan FirstTurnAllowed = TimeSpan.Zero;

        /// <summary>
        /// A time after which this initial infected player must turn.
        /// </summary>
        [DataField("firstTurnAllowed"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan TurnForced = TimeSpan.Zero;

    }
}
