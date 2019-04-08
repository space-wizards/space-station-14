using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.Components.Items
{
    [Serializable, NetSerializable]
    public class ItemComponentState : ComponentState
    {
        public string EquippedPrefix { get; set; }

        public ItemComponentState(string equippedPrefix) : base(ContentNetIDs.ITEM)
        {
            EquippedPrefix = equippedPrefix;
        }

        protected ItemComponentState(string equippedPrefix, uint netId) : base(netId)
        {
            EquippedPrefix = equippedPrefix;
        }
    }
}
