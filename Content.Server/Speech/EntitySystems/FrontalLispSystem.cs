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

        // handles ts, c(iey), z, ps, ti
        message = Regex.Replace(message, @"[T]+[S]+|[C]+(?=[IEY])|[Z]+|[P][S]+|[T](?=[I])", "TH");
        message = Regex.Replace(message, @"[Tt]+[Ss]+|[Cc]+(?=[IiEeYy])|[Zz]+|[Pp][Ss]+|[Tt](?=[Ii])", "th");
        // handles ex
        message = Regex.Replace(message, @"(?![E])[X]", "KTH");
        message = Regex.Replace(message, @"(?![Ee])[Xx]", "kth");
        // handles sth and s
        message = Regex.Replace(message, "[S]+[T]?[H]?", "TH");
        message = Regex.Replace(message, "[Ss]+[Tt]?[Hh]?", "th");

        args.Message = message;
    }
}
