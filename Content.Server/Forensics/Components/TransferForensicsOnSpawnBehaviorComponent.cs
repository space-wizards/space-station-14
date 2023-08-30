namespace Content.Server.Forensics
{
    /// <summary>
    ///     Transfers forensics from this entity to spawned objects after it's destruction
    /// </summary>
    [RegisterComponent]
    public sealed partial class TransferForensicsOnSpawnBehaviorComponent : Component
    {
        [DataField("fingersAndFibersTransferChance")]
        public float FingersAndFibersTransferChance = 0.4f;
    }
}
