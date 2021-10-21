using Robust.Shared.GameObjects;

namespace Content.Server.Devices.Components
{
    [RegisterComponent]
    public class ConstructableGrenadeComponent : Component
    {
        public override string Name => "ConstructableGrenade";

        public const string TriggerContainer = "grenadeTrigger";
    }
}
