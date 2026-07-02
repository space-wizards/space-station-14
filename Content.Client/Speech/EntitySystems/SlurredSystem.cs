using Content.Shared.Speech.EntitySystems;

namespace Content.Client.Speech.EntitySystems;

public sealed class SlurredSystem : SharedSlurredSystem
{
    public override string Accentuate(string message)
    {
        return message;
    }
}
