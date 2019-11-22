using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedMagicMirrorComponent : Component
    {
        public override string Name => "MagicMirror";

        [Serializable, NetSerializable]
        public enum MagicMirrorUiKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public class HairSelectedMessage : BoundUserInterfaceMessage
        {
            public readonly string HairName;
            public readonly bool IsFacialHair;

            public HairSelectedMessage(string name, bool isFacialHair)
            {
                HairName = name;
                IsFacialHair = isFacialHair;
            }
        }

        [Serializable, NetSerializable]
        public class HairColorSelectedMessage : BoundUserInterfaceMessage
        {
            public readonly Color HairColor;
            public readonly bool IsFacialHair;

            public HairColorSelectedMessage(Color color, bool isFacialHair)
            {
                HairColor = color;
                IsFacialHair = isFacialHair;
            }
        }
    }
}
