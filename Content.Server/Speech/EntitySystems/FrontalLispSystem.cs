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

        // replaces any instance of s/S or z/Z
        message = Regex.Replace(message, "s+", "th");
        message = Regex.Replace(message, "z+", "th");
        message = Regex.Replace(message, "S+", "TH");
        message = Regex.Replace(message, "Z+", "TH");

        args.Message = message;
    }
}
