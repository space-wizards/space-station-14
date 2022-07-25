namespace Content.Shared.Body.Events;

public sealed class BeingGibbedEvent : EntityEventArgs
{
    public readonly HashSet<EntityUid> GibbedParts;

    public BeingGibbedEvent(HashSet<EntityUid> gibbedParts)
    {
        GibbedParts = gibbedParts;
    }
}
