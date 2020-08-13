using System;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.GUI
{
    public class SharedStrippableComponent : Component
    {
        public override string Name => "Strippable";

        [NetSerializable, Serializable]
        public enum StrippingUiKey
        {
            Key,
        }
    }
}
