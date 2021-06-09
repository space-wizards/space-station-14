#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing
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
