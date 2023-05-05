using Content.Server.Explosion.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Will enable <see cref="PointLightComponent"/> on the attached entity upon a <see cref="TriggerEvent"/>.
/// </summary>
[RegisterComponent]
public sealed class PointLightEnableOnTriggerComponent : Component
{
    [DataField("removeOnTrigger")]
    public bool RemoveOnTrigger = true;
}
