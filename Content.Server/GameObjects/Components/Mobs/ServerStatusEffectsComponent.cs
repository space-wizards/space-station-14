using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

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

        public void ChangeStatusEffectIcon(StatusEffect effect, string icon)
        {
            if (_statusEffects.TryGetValue(effect, out var value) && value.Icon == icon)
            {
                return;
            }

            _statusEffects[effect] = new StatusEffectStatus()
                {Icon = icon, Cooldown = value.Cooldown};
            Dirty();
        }

        public void ChangeStatusEffectCooldown(StatusEffect effect, ValueTuple<TimeSpan, TimeSpan> cooldown)
        {
            if (_statusEffects.TryGetValue(effect, out var value)
                && value.Cooldown == cooldown)
            {
                return;
            }

            _statusEffects[effect] = new StatusEffectStatus()
            {
                Icon = value.Icon, Cooldown = cooldown
            };
            Dirty();
        }

        public void ChangeStatusEffect(StatusEffect effect, string icon, ValueTuple<TimeSpan, TimeSpan>? cooldown)
        {
            _statusEffects[effect] = new StatusEffectStatus()
                {Icon = icon, Cooldown = cooldown};
            Dirty();
        }

        public void RemoveStatusEffect(StatusEffect effect)
        {
            if (!_statusEffects.Remove(effect))
            {
                return;
            }

            Dirty();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            switch (message)
            {
                case ClickStatusMessage msg:
                {
                    var player = session.AttachedEntity;

                    if (player != Owner)
                    {
                        break;
                    }

                    // TODO: Implement clicking other status effects in the HUD
                    switch (msg.Effect)
                    {
                        case StatusEffect.Buckled:
                            if (!player.TryGetComponent(out BuckleComponent buckle))
                            {
                                break;
                            }

                            buckle.TryUnbuckle(player);
                            break;
                    }

                    break;
                }
            }
        }
    }

}
