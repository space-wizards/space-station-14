namespace Content.Server.Carrying
{
    /// <summary>
    /// Added to an entity when they are carrying somebody.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CarryingComponent : Component
    {
        public EntityUid Carried = default!;
    }
}
