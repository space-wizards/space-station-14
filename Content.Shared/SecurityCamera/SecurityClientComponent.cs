using Robust.Shared.GameObjects;

namespace Content.Shared.SecurityCamera
{
    [RegisterComponent]
    public class SecurityClientComponent : Component
    {
        public override string Name => "SecurityClient";

        public bool active;

        public int currentCamInt;
    }
}