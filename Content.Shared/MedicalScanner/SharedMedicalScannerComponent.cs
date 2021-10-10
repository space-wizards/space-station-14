using System;
using System.Collections.Generic;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner
{
    public abstract class SharedMedicalScannerComponent : Component, IDragDropOn
    {
        public override string Name => "MedicalScanner";

        [Serializable, NetSerializable]
        public class MedicalScannerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly EntityUid? Entity;
            public readonly IReadOnlyDictionary<string, int> DamagePerGroup;
            public readonly IReadOnlyDictionary<string, int> DamagePerType;
            public readonly bool IsScanned;

            public MedicalScannerBoundUserInterfaceState(
                EntityUid? entity,
                DamageableComponent? damageable,
                bool isScanned)
            {
                Entity = entity;
                DamagePerGroup = damageable?.DamagePerGroup ?? new();
                DamagePerType = damageable?.Damage?.DamageDict ?? new();
                IsScanned = isScanned;
            }

            public bool HasDamage()
            {
                return DamagePerType.Count > 0;
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

        public bool CanInsert(IEntity entity)
        {
            return entity.HasComponent<SharedBodyComponent>();
        }

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            return CanInsert(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
