#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(IMoverComponent))]
    public class SharedDummyInputMoverComponent : Component, IMoverComponent
    {
        public override string Name => "DummyInputMover";
        public bool IgnorePaused => false;
        public float CurrentWalkSpeed => 0f;
        public float CurrentSprintSpeed => 0f;

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
