using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class StutteringSystem : SharedStutteringSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        // Regex of characters to stutter.
        private static readonly Regex Stutter = new(@"[b-df-hj-np-tv-wxyz]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override void Initialize()
        {
            SubscribeLocalEvent<StutteringAccentComponent, AccentGetEvent>(OnAccent);

            SubscribeLocalEvent<StutteringAccentComponent, StatusEffectRelayedEvent<AccentGetEvent>>(OnAccent);
        }

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

        private void OnAccent(Entity<StutteringAccentComponent> entity, ref AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message, entity.Comp);
        }

        private void OnAccent(Entity<StutteringAccentComponent> entity, ref StatusEffectRelayedEvent<AccentGetEvent> args)
        {
            args.Args.Message = Accentuate(args.Args.Message, entity.Comp);
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
}
