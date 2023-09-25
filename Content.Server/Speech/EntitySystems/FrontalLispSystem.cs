using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random; // Corvax-Localization

namespace Content.Server.Speech.EntitySystems;

public sealed class FrontalLispSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrontalLispComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, FrontalLispComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // handles ts, sc(i|e|y), c(i|e|y), ps, st(io(u|n)), ch(i|e), z, s
        message = Regex.Replace(message, @"[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|([S]+[Tt]+|[T]+)(?=[Ii]+[Oo]+[Uu]*[Nn]*)|[C]+[Hh]+(?=[Ii]*[Ee]*)|[Z]+|[S]+|[X]+(?=[Ee]+)", "TH");
        message = Regex.Replace(message, @"[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|([s]+[t]+|[t]+)(?=[i]+[o]+[u]*[n]*)|[c]+[h]+(?=[i]*[e]*)|[z]+|[s]+|[x]+(?=[e]+)", "th");
        // handles ex(c), x
        message = Regex.Replace(message, @"[E]+[Xx]+[Cc]*|[X]+", "EKTH");
        message = Regex.Replace(message, @"[e]+[x]+[c]*|[x]+", "ekth");
        // Corvax-Localization Start
        // с, в, ч, т in ф or ш
        message = Regex.Replace(message, @"\B[СВЧТ]\B", _random.Prob(0.5f) ? "Ф" : "Ш");
        message = Regex.Replace(message, @"\B[свчт]\B", _random.Prob(0.5f) ? "ф" : "ш");
        // д in ф
        message = Regex.Replace(message, @"\b[Д](?![ИЕЁЮЯЬ])\b|\B[Д]\B", "Ф");
        message = Regex.Replace(message, @"\b[Дд](?![ИиЕеЁёЮюЯяЬь])\b|\B[Дд]\B", "ф");
        // Corvax-Localization End
        
        args.Message = message;
    }
}
