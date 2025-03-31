using System.Text;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class RussianAccent : IAccent
{
    public string Name { get; } = "Russian";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();

        var accentedMessage = new StringBuilder(_replacement.ApplyReplacements(message, "russian"));

        for (var i = 0; i < accentedMessage.Length; i++)
        {
            var c = accentedMessage[i];

            accentedMessage[i] = c switch
            {
                'A' => 'Д',
                'b' => 'в',
                'N' => 'И',
                'n' => 'и',
                'K' => 'К',
                'k' => 'к',
                'm' => 'м',
                'h' => 'н',
                't' => 'т',
                'R' => 'Я',
                'r' => 'я',
                'Y' => 'У',
                'W' => 'Ш',
                'w' => 'ш',
                _ => accentedMessage[i]
            };
        }

        return accentedMessage.ToString();
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        ev.Accents.Add(Name, null);
    }
}
