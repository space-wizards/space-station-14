namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public sealed partial class GhostOnMoveComponent : Component
    {
        [DataField] public bool CanReturn { get; set; } = true;

        [DataField]
        public bool MustBeDead = false;
    }
}
