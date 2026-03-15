namespace Content.Shared.SubFloor
{
    /// <summary>
    /// Raised on an entity to check if a scanner is enabled and should interact with an action.
    /// </summary>
    [Virtual]
    public class ScannerCheckEvent(EntityUid actionId) : HandledEntityEventArgs
    {
        public EntityUid ActionId { get; } = actionId;
        //public bool Denied { get; set; } = false;
    }
}
