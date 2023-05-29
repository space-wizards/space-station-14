using Content.Shared.Chemistry.Components;

namespace Content.Server.Fluids.EntitySystems
{

    [RegisterComponent]
    internal sealed class SplashOnTriggerComponent : Component
    {
        [DataField("splashReagents")] public List<Solution.ReagentQuantity> SplashReagents = new()
        {
        };
    }
}
