using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Scanner
{
    [Serializable, NetSerializable]
    public enum BodyScannerUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public class BodyScannerInterfaceState : BoundUserInterfaceState
    {
        public readonly Dictionary<string, BodyScannerBodyPartData> Parts;
        public readonly BodyScannerTemplateData Template;

        public BodyScannerInterfaceState(Dictionary<string, BodyScannerBodyPartData> parts,
            BodyScannerTemplateData template)
        {
            Template = template;
            Parts = parts;
        }
    }

    [Serializable, NetSerializable]
    public class BodyScannerBodyPartData
    {
        public readonly int CurrentDurability;
        public readonly int MaxDurability;
        public readonly List<BodyScannerMechanismData> Mechanisms;
        public readonly string Name;
        public readonly string RSIPath;
        public readonly string RSIState;

        public BodyScannerBodyPartData(string name, string rsiPath, string rsiState, int maxDurability,
            int currentDurability, List<BodyScannerMechanismData> mechanisms)
        {
            Name = name;
            RSIPath = rsiPath;
            RSIState = rsiState;
            MaxDurability = maxDurability;
            CurrentDurability = currentDurability;
            Mechanisms = mechanisms;
        }
    }

    [Serializable, NetSerializable]
    public class BodyScannerMechanismData
    {
        public readonly int CurrentDurability;
        public readonly string Description;
        public readonly int MaxDurability;
        public readonly string Name;
        public readonly string RSIPath;
        public readonly string RSIState;

        public BodyScannerMechanismData(string name, string description, string rsiPath, string rsiState,
            int maxDurability, int currentDurability)
        {
            Name = name;
            Description = description;
            RSIPath = rsiPath;
            RSIState = rsiState;
            MaxDurability = maxDurability;
            CurrentDurability = currentDurability;
        }
    }

    [Serializable, NetSerializable]
    public class BodyScannerTemplateData
    {
        public readonly string Name;
        public readonly Dictionary<string, BodyPartType> Slots;

        public BodyScannerTemplateData(string name, Dictionary<string, BodyPartType> slots)
        {
            Name = name;
            Slots = slots;
        }
    }
}
