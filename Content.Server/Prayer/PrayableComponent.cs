namespace Content.Server.Prayer
{
    /// <summary>
    /// Allows an entity to be prayed on in the context menu
    /// </summary>
    [RegisterComponent]
    public sealed class PrayableComponent : Component
    {
        /// <summary>
        /// If bible users are only allowed to use this prayable entity
        /// </summary>
        [DataField("bibleUserOnly")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool BibleUserOnly;
    }
}

