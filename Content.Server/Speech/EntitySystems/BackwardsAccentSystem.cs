using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class BackwardsAccentSystem : BaseAccentSystem<BackwardsAccentComponent>
{
    public override string Accentuate(string message, Entity<BackwardsAccentComponent>? _)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}

