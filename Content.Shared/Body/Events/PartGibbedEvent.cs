namespace Content.Shared.Body.Events
{
    /// <summary>
    /// An event raised on all the parts of an entity when it's gibbed
    /// </summary>
    public sealed class PartGibbedEvent : EntityEventArgs
    {
        public EntityUid EntityToGib;
        public readonly HashSet<EntityUid> GibbedParts;

        public PartGibbedEvent(EntityUid entityToGib, HashSet<EntityUid> gibbedParts)
        {
            EntityToGib = entityToGib;
            GibbedParts = gibbedParts;
        }
    }
}
