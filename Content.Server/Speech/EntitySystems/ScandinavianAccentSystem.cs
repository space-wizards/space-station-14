using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class ScandinavianAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly IReadOnlyDictionary<string, string[]> Letters = new Dictionary<string, string[]>()
    {
        { "W",  ["V"] },
        { "w",  ["v"] },
        { "J",  ["Y"] },
        { "j",  ["y"] },
        { "A",  ["Å","Ä","Æ","A"] },
        { "a",  ["å","ä","æ","a"] },
        { "BO", ["BJO"] },
        { "Bo", ["Bjo"] },
        { "bo", ["bjo"] },
        { "O",  ["Ö","Ø","O"] },
        { "o",  ["ö","ø","o"] },
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<ScandinavianAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        foreach (var (word, repl) in Letters)
        {
            message = message.Replace(word, _random.Pick(repl));
        }

        return message;
    }

    private void OnAccent(EntityUid uid, ScandinavianAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
