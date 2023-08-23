namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public sealed partial class GhostOnMoveComponent : Component
    {
        [DataField("canReturn")] public bool CanReturn { get; set; } = true;

        [DataField("mustBeDead")]
        public bool MustBeDead = false;
    }

    /// <summary>
    ///   Triggered when a player tries to move out of a dead body and become a ghost.
    ///   Give systems (such as PendingZombieSystem) a chance to cancel this.
    ///   Does not affect the use of the "ghost" console command.
    /// </summary>
    public sealed class GhostMoveAttempt : CancellableEntityEventArgs
    {
        public Mind.Mind Mind { get; }

        public GhostMoveAttempt(Mind.Mind mind)
        {
            Mind = mind;
        }
    }

}
