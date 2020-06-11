using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStatusEffectsComponent))]
    public sealed class ServerStatusEffectsComponent : SharedStatusEffectsComponent
    {
        private readonly Dictionary<StatusEffect, StatusEffectStatus> _statusEffects = new Dictionary<StatusEffect, StatusEffectStatus>();

        public override ComponentState GetComponentState()
        {
            return new StatusEffectComponentState(_statusEffects);
        }

        public void ChangeStatusIcon(StatusEffect effect, string icon)
        {
            if (_statusEffects.TryGetValue(effect, out var value) && value.Icon == icon)
            {
                return;
            }

            _statusEffects[effect] = new StatusEffectStatus()
                {Icon = icon, CooldownStart = value.CooldownStart, CooldownEnd = value.CooldownEnd};
            Dirty();
        }

        public void ChangeStatusCooldown(StatusEffect effect, TimeSpan? start, TimeSpan? end)
        {
            if (_statusEffects.TryGetValue(effect, out var value)
                && value.CooldownStart == start && value.CooldownEnd == end)
            {
                return;
            }

            _statusEffects[effect] = new StatusEffectStatus()
            {
                Icon = value.Icon, CooldownStart = start, CooldownEnd = end
            };
            Dirty();
        }

        public void ChangeStatus(StatusEffect effect, string icon, TimeSpan? start, TimeSpan? end)
        {
            _statusEffects[effect] = new StatusEffectStatus()
                {Icon = icon, CooldownStart = start, CooldownEnd = end};
            Dirty();
        }

        public void RemoveStatus(StatusEffect effect)
        {
            if (!_statusEffects.Remove(effect))
            {
                return;
            }

            Dirty();
        }
    }

}
