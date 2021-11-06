using System;
using Content.Server.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public class SlurredSystem : SharedSlurredSystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

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
                _statusEffectsSystem.TryAddStatusEffect<SlurredAccentComponent>(uid, SlurKey, time, status);
            else
                _statusEffectsSystem.TryAddTime(uid, SlurKey, time, status);
        }

        private void OnAccent(EntityUid uid, SlurredAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }

        public string Accentuate(string message)
        {
            return message + " slur test lol";
        }
    }
}
