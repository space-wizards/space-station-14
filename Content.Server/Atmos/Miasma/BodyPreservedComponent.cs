namespace Content.Server.Atmos.Miasma
{
    [RegisterComponent]
    /// <summary>
    /// Way for natural sources of rotting to tell if there are more unnatural preservation forces at play.
    /// </summary>
    public sealed class BodyPreservedComponent : Component
    {
        public int PreservationSources = 0;
    }
}
