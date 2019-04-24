using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;

namespace Content.Client.GameObjects.Components.Research
{
    public class MaterialStorageComponent : SharedMaterialStorageComponent
    {
        protected override Dictionary<string, int> Storage { get; set; } = new Dictionary<string, int>();

        public event Action OnMaterialStorageChanged;

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case MaterialStorageUpdateMessage msg:
                    Storage = msg.Storage;
                    OnMaterialStorageChanged?.Invoke();
                    break;

            }
        }
    }
}
