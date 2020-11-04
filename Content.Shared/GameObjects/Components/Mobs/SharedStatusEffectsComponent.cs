using System;
using System.Collections.Generic;
using Content.Shared.Status;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities
    /// </summary>
    public abstract class SharedStatusEffectsComponent : Component
    {
        [Dependency]
        protected readonly StatusEffectStateManager _statusEffectStateManager = default!;

        public override string Name => "StatusEffectsUI";
        public override uint? NetID => ContentNetIDs.STATUSEFFECTS;

        public abstract IReadOnlyDictionary<StatusEffect, StatusEffectStatus> Statuses { get; }

        public abstract void ChangeStatusEffectIcon(StatusEffect effect, string icon);

        public abstract void ChangeStatusEffect(StatusEffect effect, string icon, ValueTuple<TimeSpan, TimeSpan>? cooldown);

        public abstract void ChangeStatusEffect(string statusEffectStateId, ValueTuple<TimeSpan, TimeSpan>? cooldown = null);

        public abstract void RemoveStatusEffect(StatusEffect effect);
    }

    [Serializable, NetSerializable]
    public class StatusEffectComponentState : ComponentState
    {
        public Dictionary<StatusEffect, StatusEffectStatus> StatusEffects;

        public StatusEffectComponentState(Dictionary<StatusEffect, StatusEffectStatus> statusEffects) : base(ContentNetIDs.STATUSEFFECTS)
        {
            StatusEffects = statusEffects;
        }
    }

    /// <summary>
    /// A message that calls the click interaction on a status effect
    /// </summary>
    [Serializable, NetSerializable]
    public class ClickStatusMessage : ComponentMessage
    {
        public readonly StatusEffect Effect;

        public ClickStatusMessage(StatusEffect effect)
        {
            Directed = true;
            Effect = effect;
        }
    }

    [Serializable, NetSerializable]
    public struct StatusEffectStatus
    {
        //TODO: remove
        public string Icon;
        public int StatusEffectStateEncoded;
        // -1 if no severity
        public short Severity;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
    }

    // Each status effect is assumed to be unique
    public enum StatusEffect
    {
        Health,
        Hunger,
        Thirst,
        Pressure,
        Fire,
        Temperature,
        Stun,
        Cuffed,
        Buckled,
        Piloting,
        Pulling,
        Pulled,
        Weightless,
        Error
    }
}
