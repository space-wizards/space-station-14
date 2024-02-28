using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class VerbingAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VerbingAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, VerbingAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;
        message = Regex.Replace(message, "ing", "in'");

        args.Message = message;
    }
}
