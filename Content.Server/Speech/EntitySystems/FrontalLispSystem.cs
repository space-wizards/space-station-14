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

        // handles uppercase
        message = Regex.Replace(message, @"[S][C]|[T]?[S]+|[C](?=[IEY])|[Z]+|[P][S]+", "TH");
        //handles lowercase
        message = Regex.Replace(message, @"[Ss][Cc]|[Tt]?[Ss]+|[Cc](?=[IiEeYy])|[Zz]+|[Pp][Ss]+", "th");
    }
}
