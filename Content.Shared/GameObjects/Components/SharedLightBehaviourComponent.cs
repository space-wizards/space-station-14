#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components
{
    /// <summary>
    /// A component which applies a specific behaviour to a PointLightComponent on its owner.
    /// </summary>
    public class SharedLightBehaviourComponent : Component
    {
        public override string Name => "LightBehaviour";
    }
}
