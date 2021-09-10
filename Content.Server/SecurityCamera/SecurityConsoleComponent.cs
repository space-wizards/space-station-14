using Robust.Shared.GameObjects;
using Content.Shared.Wires;
using Content.Server.Power.Components;

namespace Content.Server.SecurityCamera
{
    [RegisterComponent]
    public class SecurityConsoleComponent : Component
    {
        public override string Name => "SecurityConsole";
        
        public ApcPowerReceiverComponent _powerReceiverComponent = default!;

        public bool Powered => _powerReceiverComponent != null && _powerReceiverComponent.Powered;
        public bool active;
    }
}