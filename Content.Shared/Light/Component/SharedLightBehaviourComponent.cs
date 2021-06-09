#nullable enable
namespace Content.Shared.Light.Component
{
    /// <summary>
    /// A component which applies a specific behaviour to a PointLightComponent on its owner.
    /// </summary>
    public class SharedLightBehaviourComponent : Robust.Shared.GameObjects.Component
    {
        public override string Name => "LightBehaviour";
    }
}
