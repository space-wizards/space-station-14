namespace Content.Server._FTL.Weapons;

/// <summary>
/// This is used for tracking weapons, specifically silos.
/// </summary>
[RegisterComponent]
public sealed class FTLWeaponSiloComponent : Component
{
    // Used solely as a work around so that openattemptevent can be used on afteropen
    public List<EntityUid>? ContainedEntities;
}
