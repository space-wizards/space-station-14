using Content.Server.Ninja.Systems;
using Content.Server.Objectives.Systems;
using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is a ninja and blew up their spider charge at its target location.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SpiderChargeSystem), typeof(SpaceNinjaSystem))]
public sealed partial class SpiderChargeConditionComponent : Component
{
    /// <summary>
    /// Warp point that the spider charge has to target
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;

    /// <summary>
    /// Tags that should be used to exclude Warp Points
    /// from the list of valid bombing targets
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
