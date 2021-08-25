using System;
using System.Collections.Generic;
using Content.Shared.Body.Components;
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
            public readonly IReadOnlyDictionary<string, int> DamagePerGroupID;
            public readonly IReadOnlyDictionary<string, int> DamagePerTypeID;
            public readonly bool IsScanned;

            public MedicalScannerBoundUserInterfaceState(
                EntityUid? entity,
                IReadOnlyDictionary<string, int> damagePerGroupID,
                IReadOnlyDictionary<string, int> damagePerTypeID,
                bool isScanned)
            {
                Entity = entity;
                DamagePerGroupID = damagePerGroupID;
                DamagePerTypeID = damagePerTypeID;
                IsScanned = isScanned;
            }

            public bool HasDamage()
            {
                return DamagePerTypeID.Count > 0;
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


        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            return eventArgs.Dragged.HasComponent<SharedBodyComponent>();
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);
    }
}
