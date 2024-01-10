using Content.Server.Objectives.Systems;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is a ninja and has converted a borg.
/// </summary>
[RegisterComponent, Access(typeof(NinjaConditionsSystem), typeof(SharedSpaceNinjaSystem))]
public sealed partial class BorgConversionConditionComponent : Component
{
    /// <summary>
    /// Whether a borg has been converted.
    /// </summary>
    [DataField]
    public bool Converted;
}
