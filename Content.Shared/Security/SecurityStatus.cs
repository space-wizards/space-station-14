namespace Content.Shared.Security;

/// <summary>
/// Status used in Criminal Records.
///
/// None - the default value
/// Suspected - the person is suspected of doing something illegal
/// Wanted - the person is being wanted by security
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
    Wanted,
    Hostile,
    Detained,
    Paroled,
    Discharged,
    Eliminated
}
