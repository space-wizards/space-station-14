using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Client.Speech.EntitySystems;

public sealed class SlurredSystem : SharedSlurredSystem
{
    public override string Accentuate(string message, Entity<SlurredAccentComponent>? ent = null)
    {
        return message;
    }
}
