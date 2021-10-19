using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Pinpointer
{
    [RegisterComponent]
    public class PinpointerComponent : Component
    {
        public override string Name => "Pinpointer";

        public EntityUid? Target = null;
        public bool IsActive = false;
        public Direction DirectionToTarget = Direction.Invalid;
    }
}
