using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires the player to be a ninja that has a spider charge target assigned, which is almost always the case.
/// </summary>
[RegisterComponent, Access(typeof(SpiderChargeTargetRequirementSystem))]
public sealed partial class SpiderChargeTargetRequirementComponent : Component
{
}
