namespace Content.Server.PowerSink
{
    /// <summary>
    /// Absorbs power up to its capacity then explodes.
    /// </summary>
    [RegisterComponent]
    public sealed class PowerSinkComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("capacity")]
        public float Capacity = 250;

        [ViewVariables]
        public float Charge = 0;

        // We definitely don't want this to explode more than once.
        public bool AlreadyExploded = false;
    }
}
