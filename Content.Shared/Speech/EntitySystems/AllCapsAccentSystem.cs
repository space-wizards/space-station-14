namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Applies the all-caps accent to speech and relayed speech status effect events.
/// </summary>
public sealed class AllCapsAccentSystem : RelayAccentSystem<Components.AllCapsAccentComponent>
{
    protected override string AccentuateInternal(EntityUid uid, Components.AllCapsAccentComponent comp, string message)
    {
        return message.ToUpperInvariant();
    }
}
