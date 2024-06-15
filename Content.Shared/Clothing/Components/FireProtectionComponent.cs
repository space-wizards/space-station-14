using Content.Shared.Clothing.EntitySystems;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Makes this clothing reduce fire damage when worn.
/// </summary>
[RegisterComponent, Access(typeof(FireProtectionSystem))]
public sealed partial class FireProtectionComponent : Component
{
    /// <summary>
    /// Percentage to reduce fire damage by, subtracted not multiplicative.
    /// 0.25 means 25% less fire damage.
    /// </summary>
    [DataField(required: true)]
    public float Reduction;
}
