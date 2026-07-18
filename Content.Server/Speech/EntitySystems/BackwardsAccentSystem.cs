using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class BackwardsAccentSystem : RelayAccentSystem<BackwardsAccentComponent>
{
    public override string Accentuate(string message, Entity<BackwardsAccentComponent>? ent = null)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}
