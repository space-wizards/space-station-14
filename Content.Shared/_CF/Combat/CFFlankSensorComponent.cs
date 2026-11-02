namespace Content.Shared.CF.Combat;

/// <summary>
/// Marks the non-solid reach circle on a combatant. The fixture id is <see cref="FixtureId"/>.
/// </summary>
[RegisterComponent]
public sealed partial class CFFlankSensorComponent : Component
{
    public const string FixtureId = "cf-flank-sensor";

    /// <summary>Reach the current circle was built for. Feet, not world units.</summary>
    public int ReachFeet;
}
