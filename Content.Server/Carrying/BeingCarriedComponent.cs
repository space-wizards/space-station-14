namespace Content.Server.Carrying
{
    /// <summary>
    /// Used so we can sub to events like VirtualItemDeleted
    /// </summary>
    [RegisterComponent]
    public sealed class BeingCarriedComponent : Component
    {
        public EntityUid Carrier = default!;
    }
}
