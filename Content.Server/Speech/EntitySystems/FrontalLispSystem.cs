using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random; // Corvax-Localization

namespace Content.Server.Speech.EntitySystems;

public sealed class FrontalLispSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization

    // @formatter:off
    private static readonly Regex RegexUpperTh = new(@"[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|([S]+[Tt]+|[T]+)(?=[Ii]+[Oo]+[Uu]*[Nn]*)|[C]+[Hh]+(?=[Ii]*[Ee]*)|[Z]+|[S]+|[X]+(?=[Ee]+)");
    private static readonly Regex RegexLowerTh = new(@"[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|([s]+[t]+|[t]+)(?=[i]+[o]+[u]*[n]*)|[c]+[h]+(?=[i]*[e]*)|[z]+|[s]+|[x]+(?=[e]+)");
    private static readonly Regex RegexUpperEcks = new(@"[E]+[Xx]+[Cc]*|[X]+");
    private static readonly Regex RegexLowerEcks = new(@"[e]+[x]+[c]*|[x]+");
    // @formatter:on

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrontalLispComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, FrontalLispComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // handles ts, sc(i|e|y), c(i|e|y), ps, st(io(u|n)), ch(i|e), z, s
        message = RegexUpperTh.Replace(message, "TH");
        message = RegexLowerTh.Replace(message, "th");
        // handles ex(c), x
        message = RegexUpperEcks.Replace(message, "EKTH");
        message = RegexLowerEcks.Replace(message, "ekth");
        // Corvax-Localization Start
        // с - ш
        message = Regex.Replace(message, @"с", _random.Prob(0.90f) ? "ш" : "с");
        message = Regex.Replace(message, @"С", _random.Prob(0.90f) ? "Ш" : "С");
        // ч - ш
        message = Regex.Replace(message, @"ч", _random.Prob(0.90f) ? "ш" : "ч");
        message = Regex.Replace(message, @"Ч", _random.Prob(0.90f) ? "Ш" : "Ч");
        // ц - ч
        message = Regex.Replace(message, @"ц", _random.Prob(0.90f) ? "ч" : "ц");
        message = Regex.Replace(message, @"Ц", _random.Prob(0.90f) ? "Ч" : "Ц");
        // т - ч
        message = Regex.Replace(message, @"\B[т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])", _random.Prob(0.90f) ? "ч" : "т");
        message = Regex.Replace(message, @"\B[Т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])", _random.Prob(0.90f) ? "Ч" : "Т");
        // з - ж
        message = Regex.Replace(message, @"з", _random.Prob(0.90f) ? "ж" : "з");
        message = Regex.Replace(message, @"З", _random.Prob(0.90f) ? "Ж" : "З");
        // Corvax-Localization End
        
        args.Message = message;
    }
}
