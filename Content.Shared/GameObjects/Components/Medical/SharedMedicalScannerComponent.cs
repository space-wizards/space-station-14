using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Medical
{
    public class SharedMedicalScannerComponent : Component
    {
        public override string Name => "MedicalScanner";

        [Serializable, NetSerializable]
        public class MedicalScannerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly int CurrentHealth;
            public readonly int MaxHealth;
            public readonly Dictionary<string, int> DamageDictionary;

            public MedicalScannerBoundUserInterfaceState(
                int currentHealth,
                int maxHealth,
                Dictionary<string, int> damageDictionary)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
                DamageDictionary = damageDictionary;
            }
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerUiKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum MedicalScannerStatus
        {
            Off,
            Open,
            Red,
            Death,
            Green,
            Yellow,
        }
    }
}
