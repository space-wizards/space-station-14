using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SouthernAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SouthernAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SouthernAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = Regex.Replace(message, "ing", "in'");
        message = Regex.Replace(message, @"\b(you all)\b", "y'all");
        message = Regex.Replace(message, @"\b(you guys)\b", "y'all");
        message = Regex.Replace(message, @"\b(is not)\b", "ain't");
        message = Regex.Replace(message, @"\b(isn't)\b", "ain't");
        args.Message = message;
    }
}
