using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class StutteringSystem : RelayAccentSystem<StutteringAccentComponent>
{
    public static readonly EntProtoId StutterEffect = "StatusEffectSlurred";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    // Regex of characters to stutter.
    private static readonly Regex Stutter = new("[b-df-hj-np-tv-wxyz]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override string Accentuate(string message, Entity<StutteringAccentComponent>? ent = null)
    {
        if (ent == null)
            return message;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value));
        var length = message.Length;
        var finalMessage = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            var newLetter = message[i].ToString();
            if (Stutter.IsMatch(newLetter) && random.Prob(ent.Value.Comp.MatchRandomProb))
            {
                if (random.Prob(ent.Value.Comp.FourRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}-{newLetter}";
                }
                else if (random.Prob(ent.Value.Comp.ThreeRandomProb))
                {
                    newLetter = $"{newLetter}-{newLetter}-{newLetter}";
                }
                else if (random.Prob(ent.Value.Comp.CutRandomProb))
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

    /// <summary>
    /// Applies or refreshes the stuttering speech status effect on an entity.
    /// </summary>
    [PublicAPI]
    public void DoStutter(EntityUid uid, TimeSpan time, bool refresh)
    {
        if (refresh)
            _statusEffects.TryUpdateStatusEffectDuration(uid, StutterEffect, time);
        else
            _statusEffects.TryAddStatusEffectDuration(uid, StutterEffect, time);
    }
}
