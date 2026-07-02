using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class StutteringSystem : SharedStutteringSystem
{
    [Dependency] private IRobustRandom _random = default!;

    // Regex of characters to stutter.
    private static readonly Regex Stutter = new(@"[b-df-hj-np-tv-wxyz]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override void DoStutter(EntityUid uid, TimeSpan time, bool refresh)
    {
        if (refresh)
            Status.TryUpdateStatusEffectDuration(uid, Stuttering, time);
        else
            Status.TryAddStatusEffectDuration(uid, Stuttering, time);
    }

    public override void DoRemoveStutterTime(EntityUid uid, TimeSpan timeRemoved)
    {
        Status.TryAddTime(uid, Stuttering, -timeRemoved);
    }

    public override void DoRemoveStutter(EntityUid uid)
    {
        Status.TryRemoveStatusEffect(uid, Stuttering);
    }

    public override string Accentuate(string message, Entity<StutteringAccentComponent>? component)
    {
        if (component == null)
            return message;

        var length = message.Length;

        var finalMessage = new StringBuilder();

        string newLetter;

        for (var i = 0; i < length; i++)
        {
            newLetter = message[i].ToString();
            if (Stutter.IsMatch(newLetter) && _random.Prob(component.Value.Comp.MatchRandomProb))
            {
                if (_random.Prob(component.Value.Comp.FourRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(component.Value.Comp.ThreeRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}";
                }
                else if (_random.Prob(component.Value.Comp.CutRandomProb))
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

