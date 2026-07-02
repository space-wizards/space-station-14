using Content.Shared.Speech.EntitySystems;

namespace Content.Client.Speech.EntitySystems;

public sealed class StutteringSystem : SharedStutteringSystem
{
    public override string Accentuate(string message)
    {
        return message;
    }
}
