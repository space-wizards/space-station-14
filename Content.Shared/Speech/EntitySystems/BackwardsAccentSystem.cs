using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class BackwardsAccentSystem : RelayAccentSystem<BackwardsAccentComponent>
{
    public override string Accentuate(string message, Entity<BackwardsAccentComponent>? ent = null)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}
