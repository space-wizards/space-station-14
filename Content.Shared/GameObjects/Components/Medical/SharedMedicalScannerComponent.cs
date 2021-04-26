#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Medical
{
    public abstract class SharedMedicalScannerComponent : Component, IDragDropOn
    {
        public override string Name => "MedicalScanner";

        [Serializable, NetSerializable]
        public class MedicalScannerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly EntityUid? Entity;
            public readonly Dictionary<DamageGroupPrototype, int> DamageGroup;
            public readonly Dictionary<DamageTypePrototype, int> DamageTypes;
            public readonly bool IsScanned;

            public MedicalScannerBoundUserInterfaceState(
                EntityUid? entity,
                Dictionary<DamageGroupPrototype, int> damageGroup,
                Dictionary<DamageTypePrototype, int> damageTypes,
                bool isScanned)
            {
                Entity = entity;
                DamageGroup = damageGroup;
                DamageTypes = damageTypes;
                IsScanned = isScanned;
            }

            public bool HasDamage()
            {
                return DamageGroup.Count > 0 || DamageTypes.Count > 0;
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

        [Serializable, NetSerializable]
        public enum UiButton
        {
            ScanDNA,
        }

        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }


        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            return eventArgs.Dragged.HasComponent<IBody>();
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
