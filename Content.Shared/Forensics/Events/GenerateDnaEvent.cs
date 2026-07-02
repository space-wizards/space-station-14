namespace Content.Shared.Forensics.Events;

/// <summary>
/// Raised on an entity when its DNA has been changed.
/// </summary>
[ByRefEvent]
public record struct GenerateDnaEvent()
{
    /// <summary>
    /// The entity getting new DNA.
    /// </summary>
    public EntityUid Owner;

    /// <summary>
    /// The generated DNA.
    /// </summary>
    public required string DNA;
}
