using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Pinpointer
{
    [RegisterComponent]
    public class PinpointerComponent : Component
    {
        public override string Name => "Pinpointer";

        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        public EntityUid? Target = null;
        public bool IsActive = false;
        public Direction DirectionToTarget = Direction.Invalid;

    }
}
