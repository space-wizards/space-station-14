using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class BackwardsAccentSystem : AccentSystem<BackwardsAccentComponent>
{
    public override string Accentuate(Entity<BackwardsAccentComponent>? entity, string message)
    {
        var arr = message.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }
}
