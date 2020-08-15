using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Health.BodySystem.BodyScanner
{


    [NetSerializable, Serializable]
    public enum BodyScannerUiKey
    {
        Key
    }

    [NetSerializable, Serializable]
    public class BodyScannerInterfaceState : BoundUserInterfaceState
    {
        public readonly Dictionary<string, BodyScannerBodyPartData> Parts;
        public readonly BodyScannerTemplateData Template;
        public BodyScannerInterfaceState(Dictionary<string, BodyScannerBodyPartData> parts, BodyScannerTemplateData template)
        {
            Template = template;
            Parts = parts;
        }
    }

    [NetSerializable, Serializable]
    public class BodyScannerBodyPartData
    {
        public readonly string Name;
        public readonly string RSIPath;
        public readonly string RSIState;
        public readonly int MaxDurability;
        public readonly int CurrentDurability;
        public readonly List<BodyScannerMechanismData> Mechanisms;
        public BodyScannerBodyPartData(string name, string rsiPath, string rsiState, int maxDurability, int currentDurability, List<BodyScannerMechanismData> mechanisms)
        {
            Name = name;
            RSIPath = rsiPath;
            RSIState = rsiState;
            MaxDurability = maxDurability;
            CurrentDurability = currentDurability;
            Mechanisms = mechanisms;
        }
    }

    [NetSerializable, Serializable]
    public class BodyScannerMechanismData
    {
        public readonly string Name;
        public readonly string Description;
        public readonly string RSIPath;
        public readonly string RSIState;
        public readonly int MaxDurability;
        public readonly int CurrentDurability;
        public BodyScannerMechanismData(string name, string description, string rsiPath, string rsiState, int maxDurability, int currentDurability)
        {
            Name = name;
            Description = description;
            RSIPath = rsiPath;
            RSIState = rsiState;
            MaxDurability = maxDurability;
            CurrentDurability = currentDurability;
        }
    }

    [NetSerializable, Serializable]
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


