namespace Content.Shared.Kitchen;

/// <summary>
/// Raised on an entity when it is inside a microwave and it starts cooking.
/// </summary>
public sealed class BeingMicrowavedEvent : HandledEntityEventArgs
{
    public EntityUid Microwave;
    public EntityUid? User;

    public BeingMicrowavedEvent(EntityUid microwave, EntityUid? user)
    {
        Microwave = microwave;
        User = user;
    }
}
