namespace Content.Shared.Kitchen;

/// <summary>
/// Raised on an entity when it is inside a microwave and it starts cooking.
/// </summary>
public sealed partial class BeingMicrowavedEvent(EntityUid microwave, EntityUid? user) : HandledEntityEventArgs
{
    public EntityUid Microwave = microwave;
    public EntityUid? User = user;
}

