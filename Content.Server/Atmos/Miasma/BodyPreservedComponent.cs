namespace Content.Server.Atmos.Miasma
{
    /// <summary>
    /// Way for natural sources of rotting to tell if there are more unnatural preservation forces at play.
    /// </summary>
    [RegisterComponent]
    public sealed class BodyPreservedComponent : Component
    {
        public int PreservationSources = 0;
    }
}
