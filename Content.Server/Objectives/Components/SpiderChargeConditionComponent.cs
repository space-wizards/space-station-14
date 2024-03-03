using Content.Server.Ninja.Systems;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is a ninja and blew up their spider charge at its target location.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SpiderChargeSystem), typeof(SpaceNinjaSystem))]
public sealed partial class SpiderChargeConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Detonated;

    /// <summary>
    /// Warp point that the spider charge has to target
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
