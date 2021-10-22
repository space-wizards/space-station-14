using Robust.Shared.GameObjects;

namespace Content.Server.Devices.Components
{
    [RegisterComponent]
    public class BombPayloadComponent : Component
    {
        public override string Name => "BombPayload";

        public const string BombPayloadContainer = "bombPayload";
    }
}
