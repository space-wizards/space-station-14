namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public sealed partial class GhostOnMoveComponent : Component
    {
        [DataField("canReturn")] public bool CanReturn { get; set; } = true;

        [DataField("mustBeDead")]
        public bool MustBeDead = false;
    }
}
