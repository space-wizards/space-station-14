using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Items
{
    [Serializable, NetSerializable]
    public class ClothingComponentState : ItemComponentState
    {
        public string ClothingEquippedPrefix { get; set; }
        public override uint NetID => ContentNetIDs.CLOTHING;

        public ClothingComponentState(string clothingEquippedPrefix, string equippedPrefix) : base(equippedPrefix)
        {
            ClothingEquippedPrefix = clothingEquippedPrefix;
        }
    }
}
