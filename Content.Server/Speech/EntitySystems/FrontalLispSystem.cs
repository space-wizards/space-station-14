using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class FrontalLispSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrontalLispComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, FrontalLispComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // handles ts, sc(i|e|y), c(i|e|y), ps, t(ion), ch(i|e), z, s
        message = Regex.Replace(message, @"[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|[T]+(?=[Ii]+[Oo]+[Nn]+)|[C]+[Hh]+(?=[Ii]+|[Ee]+)|[Z]+|[S]+", "TH");
        message = Regex.Replace(message, @"[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|[t]+(?=[i]+[o]+[n]+)|[c]+[h]+(?=[i]+|[e]+)|[z]+|[s]+", "th");
        // handles x
        message = Regex.Replace(message, @"[X]+", "KTH");
        message = Regex.Replace(message, @"[x]+", "kth");

        args.Message = message;
    }
}
