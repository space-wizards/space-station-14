using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public abstract class MumbleAccentSystem : AccentSystem<MumbleAccentComponent>
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override string Accentuate(Entity<MumbleAccentComponent>? entity, string message)
    {
        return _replacement.ApplyReplacements(message, "mumble");
    }
}
