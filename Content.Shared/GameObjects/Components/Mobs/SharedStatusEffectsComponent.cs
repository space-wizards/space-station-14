using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities
    /// </summary>
    public class SharedStatusEffectsComponent : Component
    {
        public override string Name => "StatusEffectsUI";
        public override uint? NetID => ContentNetIDs.STATUSEFFECTS;

    }
    [Serializable, NetSerializable]
    public class StatusEffectsMessage : ComponentMessage
    {
        public readonly StatusEffect Name;
        public readonly string Filepath;
        public readonly StatusEffectsMode Mode;

        public StatusEffectsMessage(StatusEffectsMode mode, StatusEffect name, string filepath)
        {
            Mode = mode;
            Name = name;
            Filepath = filepath;
            Directed = true;
        }
    }

    // Each status effect is assumed to be unique
    public enum StatusEffect
    {
        Health,
    }

    public enum StatusEffectsMode
    {
        Change,
        Remove,
    }
}
