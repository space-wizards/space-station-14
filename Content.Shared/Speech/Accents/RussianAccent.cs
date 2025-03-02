using System.Text;
using Content.Shared.Speech.EntitySystems;

namespace Content.Shared.Speech.Accents;

public sealed class RussianAccent : IAccent
{
    public string Name { get; } = "Russian";

    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Accentuate(string message, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        var replacements = _entMan.System<SharedReplacementAccentSystem>();
        var accentedMessage = new StringBuilder(replacements.ApplyReplacements(message, "russian"));

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
}
