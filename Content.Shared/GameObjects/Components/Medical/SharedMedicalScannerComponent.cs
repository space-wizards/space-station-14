using System;
using System.Collections.Generic;
using Content.Shared.Damage;
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
            public readonly EntityUid? Entity;
            public readonly Dictionary<DamageClass, int> DamageClasses;
            public readonly Dictionary<DamageType, int> DamageTypes;

            public MedicalScannerBoundUserInterfaceState(
                EntityUid? entity,
                Dictionary<DamageClass, int> damageClasses,
                Dictionary<DamageType, int> damageTypes)
            {
                Entity = entity;
                DamageClasses = damageClasses;
                DamageTypes = damageTypes;
            }

            public bool HasDamage()
            {
                return DamageClasses.Count > 0 || DamageTypes.Count > 0;
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
