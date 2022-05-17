namespace Content.Server.Medical.Components
{
    /// <summary>
    /// Adds an innate verb when equipped to use a stethoscope.
    /// </summary>
    [RegisterComponent]
    public sealed class StethoscopeComponent : Component
    {
        public bool IsActive = false;
    }
}
