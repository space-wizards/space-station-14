using Content.Server.Explosion.EntitySystems;
using Content.Shared.Singularity.Components;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Will tweak <see cref="SingularityDistortionComponent"/> on the attached entity upon a <see cref="TriggerEvent"/>.
/// </summary>
[RegisterComponent]
public sealed class SingularityDistortionOnTriggerComponent : Component
{
    [DataField("removeOnTrigger")]
    public bool RemoveOnTrigger = true;

    /// <summary>
    /// Used to tweak SingularityDistortionComponent's Intencity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("intensity")]
    public float Intensity = 1;
}
