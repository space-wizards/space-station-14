using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Applies the all-caps accent to speech and relayed speech status effect events.
/// </summary>
public sealed class AllCapsAccentSystem : RelayAccentSystem<AllCapsAccentComponent>
{
    public override string Accentuate(string message)
    {
        return message.ToUpperInvariant();
    }
}
