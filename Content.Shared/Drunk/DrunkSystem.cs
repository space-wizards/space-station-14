using System;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Drunk
{
    public class DrunkSystem : EntitySystem
    {
        public const string DrunkKey = "Drunk";

        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;

        public void TryApplyDrunkenness(EntityUid uid, float boozePower,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return;

            _slurredSystem.DoSlur(uid, TimeSpan.FromSeconds(boozePower), status);
            if (!_statusEffectsSystem.HasStatusEffect(uid, DrunkKey, status))
            {
                _statusEffectsSystem.TryAddStatusEffect<DrunkComponent>(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), status);
            }
            else
            {
                _statusEffectsSystem.TryAddTime(uid, DrunkKey, TimeSpan.FromSeconds(boozePower), status);
            }
        }
    }
}
