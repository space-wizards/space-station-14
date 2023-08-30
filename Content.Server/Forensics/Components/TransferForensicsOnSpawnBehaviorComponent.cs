namespace Content.Server.Forensics
{
    /// <summary>
    ///     Transfers forensics from destroyed object to spawned objects after destruction
    /// </summary>
    [RegisterComponent]
    public sealed partial class TransferForensicsOnSpawnBehaviorComponent : Component
    {
        [DataField("fingersAndFibersTransferChance")]
        public float FingersAndFibersTransferChance = 0.4f;
    }
}
