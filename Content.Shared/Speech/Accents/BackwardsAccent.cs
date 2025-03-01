using Robust.Shared.Random;

namespace Content.Shared.Speech.Accents;

public sealed class BackwardsAccent : IAccent
{
    public string Name { get; } = "Backwards";

    public string Accentuate(string message, int randomSeed)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}
