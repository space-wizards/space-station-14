namespace Content.Server.IgnitionSource;

    /// <summary>
    ///     Raised in order to trigger the ignitionSourceComponent on an entity
    /// </summary>
    public sealed class IgnitionEvent : EntityEventArgs
    {
        public bool Ignite { get; set; } = false;
    }

