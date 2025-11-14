using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class FrontalLispSystem : EntitySystem
{
    // @formatter:off
    private static readonly Regex RegexUpperTh = new(@"[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|([S]+[Tt]+|[T]+)(?=[Ii]+[Oo]+[Uu]*[Nn]*)|[C]+[Hh]+(?=[Ii]*[Ee]*)|[Z]+|[S]+|[X]+(?=[Ee]+)");
    private static readonly Regex RegexLowerTh = new(@"[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|([s]+[t]+|[t]+)(?=[i]+[o]+[u]*[n]*)|[c]+[h]+(?=[i]*[e]*)|[z]+|[s]+|[x]+(?=[e]+)");
    private static readonly Regex RegexUpperEcks = new(@"[E]+[Xx]+[Cc]*|[X]+");
    private static readonly Regex RegexLowerEcks = new(@"[e]+[x]+[c]*|[x]+");
    private static readonly Regex RegexUpperCyrTh = new(@"[С]+[Ц]+|[К]+[С]+(?=[ИЕЫЭЁЮЯ]+)|[Т]+[С]+|[Ц]+(?=[ИЕЫЭЁЮЯ]+)|[З]+|[С]+");
    private static readonly Regex RegexLowerCyrTh = new(@"[с]+[ц]+|[к]+[с]+(?=[иеыэёюя]+)|[т]+[с]+|[ц]+(?=[иеыэёюя]+)|[з]+|[с]+");
    private static readonly Regex RegexUpperCyrHush = new(@"[ШЩЖЧ]+");
    private static readonly Regex RegexLowerCyrHush = new(@"[шщжч]+");
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
        message = RegexUpperCyrTh.Replace(message, "ТЬ");
        message = RegexLowerCyrTh.Replace(message, "ть");
        message = RegexUpperCyrHush.Replace(message, "С");
        message = RegexLowerCyrHush.Replace(message, "с");
        // handles ex(c), x
        message = RegexUpperEcks.Replace(message, "EKTH");
        message = RegexLowerEcks.Replace(message, "ekth");

        args.Message = message;
    }
}
