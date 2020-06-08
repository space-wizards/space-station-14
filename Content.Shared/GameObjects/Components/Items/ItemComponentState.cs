using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Items
{
    [Serializable, NetSerializable]
    public class ItemComponentState : ComponentState
    {
        public string EquippedPrefix { get; set; }
        public override uint NetID => ContentNetIDs.ITEM;

        public ItemComponentState(string equippedPrefix)
        {
            EquippedPrefix = equippedPrefix;
        }

    }
}
