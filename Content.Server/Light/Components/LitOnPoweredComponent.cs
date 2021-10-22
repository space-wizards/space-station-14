using Content.Server.Light.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.Components
{
    // TODO PoweredLight also snowflakes this behavior. Ideally, powered light is renamed to 'wall light' and the
    // actual 'light on power' stuff is just handled by this component.
    /// <summary>
    ///     Enables or disables a pointlight depending on the powered
    ///     state of an entity.
    /// </summary>
    [RegisterComponent, Friend(typeof(PoweredLightSystem))]
    public class LitOnPoweredComponent : Component
    {
        public override string Name => "LitOnPowered";
    }
}
