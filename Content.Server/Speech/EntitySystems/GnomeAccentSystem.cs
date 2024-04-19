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

        // replaces no at the start of words with GNO, changed from g so words are simpler to read
        msg = Regex.Replace(msg, @"(?<!\w)\bno", "GNO", RegexOptions.IgnoreCase);
        // replaces certain past tense words with GNOMED 
        msg = Regex.Replace(msg, @"(?<!\w)\bfuck you", "GET GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bshitters", "GNOMERS", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bfucked", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bkilled", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bdead", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bshot", "GNOMED", RegexOptions.IgnoreCase);
        msg = Regex.Replace(msg, @"(?<!\w)\bstabbed", "GNOMED", RegexOptions.IgnoreCase);
        //various other replacements to get a more gnomeish "feel" :3 the ones below this are capitalized, that is just to make it work
        //TODO: make this work without ignoring the case
        msg = Regex.Replace(msg, @"(?<!\w)\bmy", "mi", RegexOptions.None);
        msg = Regex.Replace(msg, @"(?<!\w)\bfriend", "chum", RegexOptions.None);
        msg = Regex.Replace(msg, @"(?<!\w)\bfriends", "chums", RegexOptions.None);
        return msg;
    }
    

    private void OnAccentGet(EntityUid uid, GnomeAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
