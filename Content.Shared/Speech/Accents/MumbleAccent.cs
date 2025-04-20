using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class MumbleAccent : IAccent
{
    public string Name { get; } = "Lizard";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();
        return _replacement.ApplyReplacements(message, "mumble");
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
