namespace Content.Server.Carrying
{
    /// <summary>
    /// Stores the carrier of an entity being carried.
    /// </summary>
    [RegisterComponent]
    public sealed class BeingCarriedComponent : Component
    {
        public EntityUid Carrier = default!;
    }
}
