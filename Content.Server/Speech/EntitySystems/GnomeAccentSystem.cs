using Content.Server.Speech.Components;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// System that Gnomes the Gnomes talking
/// </summary>
public sealed class GnomeAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;  

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GnomeAccentComponent, AccentGetEvent>(OnAccentGet);
    }
    public string Accentuate(string message, GnomeAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "gnome");

        // replaces g at the start of words with GN
        msg = Regex.Replace(msg, @"(?<!\w)\bg", "GN", RegexOptions.IgnoreCase);
        // replaces certain past tense words with GNOMED 
        msg = Regex.Replace(msg, @"(?<!\w)\bfuck you", "GET GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bshitters", "GNOMERS", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bfucked", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bkilled", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bdead", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bshot", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bstabbed", "GNOMED", RegexOptions.IgnoreCase);

        return msg;
    }
    

    private void OnAccentGet(EntityUid uid, GnomeAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
