#nullable enable
using System;
using Robust.Shared.GameObjects;
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
            public (byte r, byte g, byte b) HairColor;
            public readonly bool IsFacialHair;

            public HairColorSelectedMessage((byte r, byte g, byte b) color, bool isFacialHair)
            {
                HairColor = color;
                IsFacialHair = isFacialHair;
            }
        }

        [Serializable, NetSerializable]
        public class MagicMirrorInitialDataMessage : BoundUserInterfaceMessage
        {
            public readonly Color HairColor;
            public readonly Color FacialHairColor;
            public readonly string HairName;
            public readonly string FacialHairName;

            public MagicMirrorInitialDataMessage(Color hairColor, Color facialHairColor, string hairName, string facialHairName)
            {
                HairColor = hairColor;
                FacialHairColor = facialHairColor;
                HairName = hairName;
                FacialHairName = facialHairName;
            }
        }
    }
}
