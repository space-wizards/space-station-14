using Content.Server.Ninja.Systems;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is a ninja and blew up their spider charge at its target location.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SpiderChargeSystem))]
public sealed partial class SpiderChargeConditionComponent : Component
{
    [DataField("spiderChargeDetonated"), ViewVariables(VVAccess.ReadWrite)]
    public bool SpiderChargeDetonated;
}
