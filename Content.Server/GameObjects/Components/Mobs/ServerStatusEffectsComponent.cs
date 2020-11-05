using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Status;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStatusEffectsComponent))]
    public sealed class ServerStatusEffectsComponent : SharedStatusEffectsComponent
    {
        [ViewVariables]
        private readonly Dictionary<StatusEffect, StatusEffectStatus> _statusEffects = new Dictionary<StatusEffect, StatusEffectStatus>();

        public override IReadOnlyDictionary<StatusEffect, StatusEffectStatus> Statuses => _statusEffects;

        protected override void Startup()
        {
            base.Startup();

            EntitySystem.Get<WeightlessSystem>().AddStatus(this);
        }

        public override void OnRemove()
        {
            EntitySystem.Get<WeightlessSystem>().RemoveStatus(this);

            base.OnRemove();
        }

        public override ComponentState GetComponentState()
        {
            return new StatusEffectComponentState(_statusEffects);
        }

        /// <inheritdoc />
        public override void ChangeStatusEffectIcon(string statusEffectStateId, short? severity = null)
        {
            if (_statusEffectStateManager.TryGetWithEncoded(statusEffectStateId, out var statusEffectState, out var encoded))
            {
                if (_statusEffects.TryGetValue(statusEffectState.StatusEffect, out var value) && value.StatusEffectStateEncoded == encoded
                    && value.Severity == severity)
                {
                    return;
                }

                _statusEffects[statusEffectState.StatusEffect] = new StatusEffectStatus()
                    { Cooldown = value.Cooldown, StatusEffectStateEncoded = encoded, Severity = severity};
                Dirty();
            }
            else
            {
                Logger.ErrorS("status", "Unable to set status effect state {0}, please ensure this is a valid statusEffectState",
                    statusEffectStateId);
            }


        }

        /// <inheritdoc />
        public override void ChangeStatusEffect(string statusEffectStateId, short? severity = null, (TimeSpan, TimeSpan)? cooldown = null)
        {
            if (_statusEffectStateManager.TryGetWithEncoded(statusEffectStateId, out var statusEffectState, out var encoded))
            {
                if (_statusEffects.TryGetValue(statusEffectState.StatusEffect, out var value) && value.StatusEffectStateEncoded == encoded
                    && value.Severity == severity && value.Cooldown == cooldown)
                {
                    return;
                }

                _statusEffects[statusEffectState.StatusEffect] = new StatusEffectStatus()
                    {Cooldown = cooldown, StatusEffectStateEncoded = encoded, Severity = severity};
                Dirty();
            }
            else
            {
                Logger.ErrorS("status", "Unable to set status effect state {0}, please ensure this is a valid statusEffectState",
                    statusEffectStateId);
            }
        }

        public override void RemoveStatusEffect(StatusEffect effect)
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
                                break;

                            buckle.TryUnbuckle(player);
                            break;
                        case StatusEffect.Piloting:
                            if (!player.TryGetComponent(out ShuttleControllerComponent controller))
                                break;

                            controller.RemoveController();
                            break;
                        case StatusEffect.Pulling:
                            EntitySystem
                                .Get<SharedPullingSystem>()
                                .GetPulled(player)?
                                .GetComponentOrNull<SharedPullableComponent>()?
                                .TryStopPull();

                            break;
                        case StatusEffect.Fire:
                            if (!player.TryGetComponent(out FlammableComponent flammable))
                                break;

                            flammable.Resist();
                            break;
                        default:
                            player.PopupMessage(msg.Effect.ToString());
                            break;
                    }

                    break;
                }
            }
        }
    }

}
