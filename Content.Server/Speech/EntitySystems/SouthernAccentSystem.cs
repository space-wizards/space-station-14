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

        //nothin'
        message = Regex.Replace(message, "ing", "in'");

        //coulda
        message = Regex.Replace(message, "d've", "da");

        message = Regex.Replace(message, "and", "an'");
        
        message = Regex.Replace(message, "you all", "y'all");
        message = Regex.Replace(message, "you guys", "y'all");

        message = Regex.Replace(message, "is not", "ain't");
        message = Regex.Replace(message, "isn't", "ain't");

        args.Message = message;
    }
}
