using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class BackwardsAccent : IAccent
{
    public string Name { get; } = "Backwards";

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
