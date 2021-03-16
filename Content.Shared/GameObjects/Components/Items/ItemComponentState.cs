#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Items
{
    [Serializable, NetSerializable]
    public class ItemComponentState : ComponentState
    {
        public string? EquippedPrefix { get; set; }

        public ItemComponentState(string? equippedPrefix) : base(ContentNetIDs.ITEM)
        {
            EquippedPrefix = equippedPrefix;
        }

        protected ItemComponentState(string? equippedPrefix, uint netId) : base(netId)
        {
            EquippedPrefix = equippedPrefix;
        }
    }
}
