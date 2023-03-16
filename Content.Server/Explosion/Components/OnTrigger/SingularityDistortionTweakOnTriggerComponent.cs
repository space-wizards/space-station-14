using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Will enable <see cref="SingularityDistortionComponent"/> on the attached entity upon a <see cref="TriggerEvent"/>.
    /// </summary>
    [RegisterComponent]
    public sealed class SingularityDistortionTweakOnTriggerComponent : Component
    {
        [DataField("removeOnTrigger")]
        public bool RemoveOnTrigger = false;

        /// <summary>
        /// Used to tweak SingularityDistortionComponent's Intencity
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("intensity")]
        public float Intensity = 1;
    }
}
