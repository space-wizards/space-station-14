namespace Content.Shared.Antag;

/// <summary>
/// Used by AntagSelectionSystem to indicate which types of antag roles are allowed to choose the same entity
/// For example, Thief HeadRev
/// </summary>
public enum AntagAcceptability
{
    /// <summary>
    /// Dont choose anyone who already has an antag role
    /// </summary>
    None,
    /// <summary>
    /// Dont choose anyone who has an exclusive antag role
    /// </summary>
    NotExclusive,
    /// <summary>
    /// Choose anyone
    /// </summary>
    All
}

