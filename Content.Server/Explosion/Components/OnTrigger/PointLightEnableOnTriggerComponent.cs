using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Will enable <see cref="SharedPointLightComponent"/> on the attached entity upon a <see cref="TriggerEvent"/>.
    /// </summary>
    [RegisterComponent]
    public sealed class PointLightEnableOnTriggerComponent : Component
    {
        [DataField("removeOnTrigger")]
        public bool RemoveOnTrigger = false;
    }
}
