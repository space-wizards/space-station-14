using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class SouthernAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SouthernAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SouthernAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "southern");

        //They shoulda started runnin' an' hidin' from me!
        message = Regex.Replace(message, @"ing\b", "in'");
        message = Regex.Replace(message, @"ING\b", "IN'");

        message = Regex.Replace(message, @"\band\b", "an'");
        message = Regex.Replace(message, @"\bAnd\b", "An'");
        message = Regex.Replace(message, @"\bAND\b", "AN'");

        message = Regex.Replace(message, "d've", "da");
        message = Regex.Replace(message, "D'VE", "DA");

        args.Message = message;
    }
};
