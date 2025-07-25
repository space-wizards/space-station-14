namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised directed at two entities to indicate that they came into contact, usually as a result of some other interaction.
/// </summary>
/// <remarks>
///     This is currently used by the forensics and disease systems to perform on-contact interactions.
/// </remarks>
public sealed class ContactInteractionEvent : HandledEntityEventArgs
{
    public EntityUid Other;

    public ContactInteractionEvent(EntityUid other)
    {
        Other = other;
    }
}
