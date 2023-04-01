using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Speech.EntitySystems;

public sealed class ChatFilterAccentSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "fuck", "####" },
        { "shit", "####" },
        { "ass", "###" },
        { "dick", "####" },
        { "bitch", "#####" },
        { "piss", "####" },
        { "damn", "####" },
        { "kill", "####" },
        { "hurt", "####" },
        { "god", "###" },
        { "hell", "####" },
        { "nuke", "####" },
        { "dang", "####" },
        { "lol", "###" },
        { "nukies", "#####" },
        { "nukie", "#####" },
        { "gun", "###" },
        { "ammo", "####" },
        { "bridge", "#####" },
        { "med", "###" },
        { "clown", "#####" },
        { "admin", "#####" },
        { "badmin", "######" },
        { "admeme", "######" },
        { "badmeme", "######" },
        { "centcom", "#######" },
        { "singulo", "#######" },
        { "singuloose", "##########" },
        { "sword", "#####" },
        { "dead", "####" },
        { "died", "####" },
        { "sus", "###" },
        { "aos", "###" },
        { "kos", "###" },
        { "anomaly", "#######" },
        { "shuttle", "#######" },
        { "hos", "###" },
        { "drunk", "#####" },
        { "bar", "###" },
        { "arrest", "#####" },
        { "bomb", "####" },
        { "I", "#" },
        { "traitor", "#######" },
        { "tator", "#####" },
        { "bad", "###" },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChatFilterAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message)
    {
        var msg = message;

        if (_cfg.GetCVar(CCVars.GameChatFilter) == true)
        {

            foreach (var (first, replace) in DirectReplacements)
            {
                msg = Regex.Replace(msg, $@"{first}", replace, RegexOptions.IgnoreCase);
            }

            return msg;
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, ChatFilterAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
