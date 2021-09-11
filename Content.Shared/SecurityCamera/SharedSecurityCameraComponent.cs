using Robust.Shared.GameObjects;
namespace Content.Shared.SecurityCamera
{
    public class SharedSecurityCameraComponent : Component
    {
        public override string Name => "SecurityCamera";

        public bool Connected = true;
    }
}