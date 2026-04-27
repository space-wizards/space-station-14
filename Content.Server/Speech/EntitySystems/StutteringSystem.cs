using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class StutteringSystem : SharedStutteringSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // Regex of characters to stutter.
    private static readonly Regex Stutter = new(@"[b-df-hj-np-tv-wxyz]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override void DoStutter(EntityUid uid, TimeSpan time, bool refresh)
    {
        if (refresh)
            Status.TryUpdateStatusEffectDuration(uid, SharedStutteringSystem.Stuttering, time);
        else
            Status.TryAddStatusEffectDuration(uid, SharedStutteringSystem.Stuttering, time);
    }

    public override void DoRemoveStutterTime(EntityUid uid, TimeSpan timeRemoved)
    {
        Status.TryAddTime(uid, SharedStutteringSystem.Stuttering, -timeRemoved);
    }

    public override void DoRemoveStutter(EntityUid uid)
    {
        Status.TryRemoveStatusEffect(uid, SharedStutteringSystem.Stuttering);
    }

    protected override string AccentuateInternal(EntityUid uid, StutteringAccentComponent comp, string message)
    {
        return Accentuate(message, comp);
    }

    public string Accentuate(string message, StutteringAccentComponent component)
    {
        var length = message.Length;

        var finalMessage = new StringBuilder();

        string newLetter;

        for (var i = 0; i < length; i++)
        {
            newLetter = message[i].ToString();
            if (Stutter.IsMatch(newLetter) && _random.Prob(component.MatchRandomProb))
            {
                if (_random.Prob(component.FourRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(component.ThreeRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(component.CutRandomProb))
                {
                    newLetter = "";
                }
                else
                {
                    newLetter = $"{newLetter}-{newLetter}";
                }
            }

            finalMessage.Append(newLetter);
        }

        return finalMessage.ToString();
    }
}
