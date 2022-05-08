namespace Content.Server.PowerSink
{
    /// <summary>
    /// Absorbs power up to its capacity then explodes.
    /// </summary>
    [RegisterComponent]
    public sealed class PowerSinkComponent : Component
    {
        public bool IsAnchored;
    }
}
