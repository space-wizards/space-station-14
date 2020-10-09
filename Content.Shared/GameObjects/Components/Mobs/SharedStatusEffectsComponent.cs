using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities
    /// </summary>
    public abstract class SharedStatusEffectsComponent : Component
    {
        public override string Name => "StatusEffectsUI";
        public override uint? NetID => ContentNetIDs.STATUSEFFECTS;

        public abstract void ChangeStatusEffect(StatusEffect effect, string icon, ValueTuple<TimeSpan, TimeSpan>? cooldown);
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
        public string Icon;
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
        Pulled
    }
}
