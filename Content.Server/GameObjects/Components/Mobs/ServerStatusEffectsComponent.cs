using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStatusEffectsComponent))]
    public sealed class ServerStatusEffectsComponent : SharedStatusEffectsComponent
    {
        private readonly Dictionary<StatusEffect, string> _statusEffects = new Dictionary<StatusEffect, string>();

        public override ComponentState GetComponentState()
        {
            return new StatusEffectComponentState(_statusEffects);
        }

        public void ChangeStatus(StatusEffect effect, string icon)
        {
            if (_statusEffects.TryGetValue(effect, out string value) && value == icon)
            {
                return;
            }

            _statusEffects[effect] = icon;
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
