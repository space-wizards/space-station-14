using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.SecurityCamera
{
    [RegisterComponent]
    public class ClientSecurityCameraComponent : Component
    {
        public override string Name => "ClientSecurityCamera";

        [ViewVariables]
        public bool Connected;
    }
}