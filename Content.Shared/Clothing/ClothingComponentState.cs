using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing
{
    [Serializable, NetSerializable]
    public class ClothingComponentState : ComponentState
    {
        public string? ClothingEquippedPrefix { get; }

        public ClothingComponentState(string? clothingEquippedPrefix)
        {
            ClothingEquippedPrefix = clothingEquippedPrefix;
        }
    }
}
