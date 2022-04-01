using System;
using System.Text;
using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SlurredSystem : SharedSlurredSystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const string SlurKey = "SlurredSpeech";

        public override void Initialize()
        {
            SubscribeLocalEvent<SlurredAccentComponent, AccentGetEvent>(OnAccent);
        }

        public override void DoSlur(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return;

            if (!_statusEffectsSystem.HasStatusEffect(uid, SlurKey, status))
                _statusEffectsSystem.TryAddStatusEffect<SlurredAccentComponent>(uid, SlurKey, time, true, status);
            else
                _statusEffectsSystem.TryAddTime(uid, SlurKey, time, status);
        }

        private void OnAccent(EntityUid uid, SlurredAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }

        private string Accentuate(string message)
        {
            var sb = new StringBuilder();

            // This is pretty much ported from TG.
            foreach (var character in message)
            {
                if (_random.Prob(1f / 3f))
                {
                    var lower = Char.ToLowerInvariant(character);
                    var newString = lower switch
                    {
                        'o' => "u",
                        's' => "ch",
                        'a' => "ah",
                        'u' => "oo",
                        'c' => "k",
                        _ => $"{character}",
                    };

                    sb.Append(newString);
                }

                if (_random.Prob(1f / 20f))
                {
                    if (character == ' ')
                    {
                        sb.Append(Loc.GetString("slur-accent-confused"));
                    }
                    else if (character == '.')
                    {
                        sb.Append(' ');
                        sb.Append(Loc.GetString("slur-accent-burp"));
                    }
                }

                var randInt = _random.Next(1, 20);
                var next = randInt switch
                {
                    1 => "'",
                    10 => $"{character}{character}",
                    20 => $"{character}{character}{character}",
                    _ => $"{character}",
                };

                sb.Append(next);
            }

            return sb.ToString();
        }
    }
}
