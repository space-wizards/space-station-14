using Robust.Shared.GameObjects;

namespace Content.Server.SecurityCamera
{
    [RegisterComponent]
    public class SecurityConsoleComponent : Component
    {
        public override string Name => "SecurityConsole";
        
        public bool active;
    }
}