namespace Content.Shared.Security;

/// <summary>
/// Status used in Criminal Records.
///
/// None - the default value
/// Suspected - the person is suspected of doing something illegal
/// Charged - the person has been charged with a crime and should be arrested
/// Wanted - the person has evaded arrest and should be arrested on sight
/// Hostile - the person has been admitted as hostile
/// Detained - the person is detained by security
/// Paroled - the person is on parole
/// Discharged - the person has been released from prison
/// Eliminated - the person has been eliminated and should not be healed
/// </summary>
public enum SecurityStatus : byte
{
    None,
    Suspected,
    Charged,
    Wanted,
    Hostile,
    Detained,
    Paroled,
    Discharged,
    Eliminated
}
