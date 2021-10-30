using Robust.Shared.GameObjects;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalSwitchComponent : Component
    {
        public override string Name => "SignalSwitch";

        public bool State;
    }
}
