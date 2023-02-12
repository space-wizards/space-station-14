namespace Content.Server.Body.Components;

public sealed class BeingGibbedEvent : EntityEventArgs
{
    public readonly HashSet<EntityUid> GibbedParts;

    public BeingGibbedEvent(HashSet<EntityUid> gibbedParts)
    {
        GibbedParts = gibbedParts;
    }
}
