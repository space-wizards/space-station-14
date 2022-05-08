namespace Content.Server.PowerSink
{
    /// <summary>
    /// Absorbs power up to its capacity then explodes.
    /// </summary>
    [RegisterComponent]
    public sealed class PowerSinkComponent : Component
    {
        // We definitely don't want this to explode more than once.
        public bool AlreadyExploded = false;
    }
}
