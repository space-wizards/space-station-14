using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class StutteringSystem : SharedStutteringSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        // Regex of characters to stutter.
        private static readonly Regex StutterRegex = new(@"[b-df-hj-np-tv-wxyz]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override void Initialize()
        {
            SubscribeLocalEvent<StutteringAccentComponent, AccentGetEvent>(OnAccent);
            SubscribeLocalEvent<StutterStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
            SubscribeLocalEvent<StutterStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
        }

        public override void DoStutter(EntityUid uid, TimeSpan time, bool refresh)
        {
            Status.TryAddStatusEffect(uid, Stutter, time, refresh);
        }

        public override void DoRemoveStutterTime(EntityUid uid, TimeSpan timeRemoved)
        {
            Status.TryAddTime(uid, Stutter, -timeRemoved);
        }

        public override void DoRemoveStutter(EntityUid uid)
        {
            Status.TryRemoveStatusEffect(uid, Stutter);
        }

        private void OnStatusApplied(Entity<StutterStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
        {
            EnsureComp<StutteringAccentComponent>(args.Target);
        }

        private void OnStatusRemoved(Entity<StutterStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
        {
            if(!Status.HasEffectComp<StutterStatusEffectComponent>(args.Target))
                RemComp<StutteringAccentComponent>(args.Target);
        }

        private void OnAccent(EntityUid uid, StutteringAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message, component);
        }

        public string Accentuate(string message, StutteringAccentComponent component)
        {
            var length = message.Length;

            var finalMessage = new StringBuilder();

            string newLetter;

            for (var i = 0; i < length; i++)
            {
                newLetter = message[i].ToString();
                if (StutterRegex.IsMatch(newLetter) && _random.Prob(component.MatchRandomProb))
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
