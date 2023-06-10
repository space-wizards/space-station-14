namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public sealed class GhostOnMoveComponent : Component
    {
        [DataField("canReturn")] public bool CanReturn { get; set; } = true;

        [DataField("mustBeDead")]
        public bool MustBeDead = false;
    }

    public sealed class GhostMoveAttempt : CancellableEntityEventArgs
    {
        public Mind.Mind Mind { get; }

        public GhostMoveAttempt(Mind.Mind mind)
        {
            Mind = mind;
        }
    }

}
