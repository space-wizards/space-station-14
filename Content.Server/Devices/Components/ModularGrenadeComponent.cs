using Robust.Shared.GameObjects;

namespace Content.Server.Devices.Components
{
    [RegisterComponent]
    public class ModularGrenadeComponent : Component
    {
        public override string Name => "ModularGrenade";

        public const string TriggerContainer = "grenadeTrigger";
    }
}
