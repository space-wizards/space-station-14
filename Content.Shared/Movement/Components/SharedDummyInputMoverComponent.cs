namespace Content.Shared.Movement.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public sealed class SharedDummyInputMoverComponent : Component, IMoverComponent
    {
        public bool IgnorePaused => false;

        public bool CanMove { get; set; } = true;

        public Angle LastGridAngle { get => Angle.Zero; set {} }

        public bool Sprinting => false;
        public (Vector2 walking, Vector2 sprinting) VelocityDir => (Vector2.Zero, Vector2.Zero);

        public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled)
        {
        }

        public void SetSprinting(ushort subTick, bool walking)
        {
        }
    }
}
