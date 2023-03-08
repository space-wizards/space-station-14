using System.Globalization;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class PirateAccentSystem : EntitySystem
{
    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { (Loc.GetString($"accent-pirate-word-1")), (Loc.GetString($"accent-pirate-response-1")) },
        { (Loc.GetString($"accent-pirate-word-2")), (Loc.GetString($"accent-pirate-response-1")) },
        { (Loc.GetString($"accent-pirate-word-3")), (Loc.GetString($"accent-pirate-response-2")) },
        { (Loc.GetString($"accent-pirate-word-4")), (Loc.GetString($"accent-pirate-response-3")) },
        { (Loc.GetString($"accent-pirate-word-5")), (Loc.GetString($"accent-pirate-response-4")) },
        { (Loc.GetString($"accent-pirate-word-6")), (Loc.GetString($"accent-pirate-response-5")) },
        { (Loc.GetString($"accent-pirate-word-7")), (Loc.GetString($"accent-pirate-response-6")) },
        { (Loc.GetString($"accent-pirate-word-8")), (Loc.GetString($"accent-pirate-response-7")) },
        { (Loc.GetString($"accent-pirate-word-9")), (Loc.GetString($"accent-pirate-response-8")) },
        { (Loc.GetString($"accent-pirate-word-10")), (Loc.GetString($"accent-pirate-response-9")) },
        { (Loc.GetString($"accent-pirate-word-11")), (Loc.GetString($"accent-pirate-response-10")) },
        { (Loc.GetString($"accent-pirate-word-12")), (Loc.GetString($"accent-pirate-response-11")) },
        { (Loc.GetString($"accent-pirate-word-13")), (Loc.GetString($"accent-pirate-response-12")) },
        { (Loc.GetString($"accent-pirate-word-14")), (Loc.GetString($"accent-pirate-response-12")) },
        { (Loc.GetString($"accent-pirate-word-15")), (Loc.GetString($"accent-pirate-response-13")) },
        { (Loc.GetString($"accent-pirate-word-16")), (Loc.GetString($"accent-pirate-response-19")) },
        { (Loc.GetString($"accent-pirate-word-17")), (Loc.GetString($"accent-pirate-response-15")) },
        { (Loc.GetString($"accent-pirate-word-18")), (Loc.GetString($"accent-pirate-response-14")) },
        { (Loc.GetString($"accent-pirate-word-19")), (Loc.GetString($"accent-pirate-response-16")) },
        { (Loc.GetString($"accent-pirate-word-20")), (Loc.GetString($"accent-pirate-response-17")) },
        { (Loc.GetString($"accent-pirate-word-21")), (Loc.GetString($"accent-pirate-response-18")) },
        { (Loc.GetString($"accent-pirate-word-22")), (Loc.GetString($"accent-pirate-response-20")) },
        { (Loc.GetString($"accent-pirate-word-23")), (Loc.GetString($"accent-pirate-response-21")) },
        { (Loc.GetString($"accent-pirate-word-24")), (Loc.GetString($"accent-pirate-response-22")) },
        { (Loc.GetString($"accent-pirate-word-25")), (Loc.GetString($"accent-pirate-response-23")) },
        { (Loc.GetString($"accent-pirate-word-26")), (Loc.GetString($"accent-pirate-response-24")) },
        { (Loc.GetString($"accent-pirate-word-27")), (Loc.GetString($"accent-pirate-response-25")) },
        { (Loc.GetString($"accent-pirate-word-28")), (Loc.GetString($"accent-pirate-response-26")) },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PirateAccentComponent, AccentGetEvent>(OnAccentGet);
    }
    private void OnAccentGet(EntityUid uid, PirateAccentComponent component, AccentGetEvent args)
    {
        foreach (var (first, replace) in DirectReplacements)
        {
            args.Message = Regex.Replace(args.Message, $@"(?<!\w){first}(?!\w)", replace, RegexOptions.IgnoreCase);
        }
    }
}
