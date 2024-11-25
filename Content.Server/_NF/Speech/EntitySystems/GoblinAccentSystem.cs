using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Speech.EntitySystems;

// The whole code is a copy of SouthernAccentSystem by UBlueberry (https://github.com/UBlueberry)
public sealed class GoblinAccentSystem : EntitySystem
{
    private static readonly Regex RegexIng = new(@"(in)g\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexAnd = new(@"\b(an)d\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexEr = new(@"([^\WpPfF])er\b"); // Keep "er", "per", "Per", "fer" and "Fer"
    private static readonly Regex RegexErUpper = new(@"([^\WpPfF])ER\b"); // Keep "ER", "PER" and "FER"
    private static readonly Regex RegexTwoLetterEr = new(@"(\w\w)er\b"); // Replace "..XXer", e.g. "super"->"supah"
    private static readonly Regex RegexTwoLetterErUpper = new(@"(\w\w)ER\b"); // Replace "..XXER", e.g. "SUPER"->"SUPAH"
    private static readonly Regex RegexErs = new(@"(\w)ers\b"); // Replace "..XXers", e.g. "fixers"->"fixas"
    private static readonly Regex RegexErsUpper = new(@"(\w)ERS\b"); // Replace "..XXers", e.g. "fixers"->"fixas"
    private static readonly Regex RegexTt = new(@"([aeiouy])tt", RegexOptions.IgnoreCase);
    private static readonly Regex RegexOf = new(@"\b(o)f\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThe = new(@"\bthe\b");
    private static readonly Regex RegexTheUpper = new(@"\bTHE\b");
    private static readonly Regex RegexH = new(@"\bh", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSelf = new(@"self\b");
    private static readonly Regex RegexSelfUpper = new(@"SELF\b");

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GoblinAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GoblinAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "goblin_accent");

        message = RegexIng.Replace(message, "$1'"); //ing->in', ING->IN'
        message = RegexAnd.Replace(message, "$1'"); //and->an', AND->AN'
        message = RegexEr.Replace(message, "$1ah");
        message = RegexErUpper.Replace(message, "$1AH");
        message = RegexTwoLetterEr.Replace(message, "$1ah");
        message = RegexTwoLetterErUpper.Replace(message, "$1AH");
        message = RegexErs.Replace(message, "$1as");
        message = RegexErsUpper.Replace(message, "$1AS");
        message = RegexTt.Replace(message, "$1'");
        message = RegexH.Replace(message, "'");
        message = RegexSelf.Replace(message, "sewf");
        message = RegexSelfUpper.Replace(message, "SEWF");
        message = RegexOf.Replace(message, "$1'"); //of->o', OF->O'
        message = RegexThe.Replace(message, "da");
        message = RegexTheUpper.Replace(message, "DA");

        args.Message = message;
    }
};
