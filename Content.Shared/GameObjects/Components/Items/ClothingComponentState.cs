#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Items
{
    [Serializable, NetSerializable]
    public class ClothingComponentState : ComponentState
    {
        public string? ClothingEquippedPrefix { get; }

        public string? EquippedPrefix { get; }

        public ClothingComponentState(string? clothingEquippedPrefix, string? equippedPrefix) : base(ContentNetIDs.CLOTHING)
        {
            ClothingEquippedPrefix = clothingEquippedPrefix;
            EquippedPrefix = equippedPrefix;
        }
    }
}
