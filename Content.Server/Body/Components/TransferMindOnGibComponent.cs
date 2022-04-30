namespace Content.Server.Body.Components
{
    [RegisterComponent]
    public sealed class TransferMindOnGibComponent : Component
    {
        /// <summary>
        /// The entity the mind will be transferred to
        /// stored in here for use in system
        /// </summary>
        [ViewVariables]
        public EntityUid? TransferTarget = null;
    }
}
