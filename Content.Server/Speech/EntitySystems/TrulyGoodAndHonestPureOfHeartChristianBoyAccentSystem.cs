using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// This handles the logic and function by which the possesor of the
/// <see cref="TrulyGoodAndHonestPureOfHeartChristianBoyAccentComponent"/>
/// can use to absolve themselves of the ability to commit sin and hate unto
/// god's pure and lovely world.
/// </summary>
public sealed class TrulyGoodAndHonestPureOfHeartChristianBoyAccentSystem : EntitySystem
{
    private static readonly Dictionary<string, string> DirectReplacements = new()
    {
        { "fuck", "frick" },
        { "shit", "poop" },
        { "ass", "butt" },
        { "dick", "peter-pecker" },
        { "bitch", "nice woman" },
        { "piss", "pee" },
        { "damn", "beaver dam" },
        { "kill", "love" },
        { "hurt", "love" },
        { "god", "God" },
        { "hell", "the evil home of the enemy of god" }
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrulyGoodAndHonestPureOfHeartChristianBoyAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message)
    {
        var msg = message;

        foreach (var (first, replace) in DirectReplacements)
        {
            msg = Regex.Replace(msg, $@"{first}", replace, RegexOptions.IgnoreCase);
        }

        return msg;
    }

    private void OnAccentGet(EntityUid uid, TrulyGoodAndHonestPureOfHeartChristianBoyAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
