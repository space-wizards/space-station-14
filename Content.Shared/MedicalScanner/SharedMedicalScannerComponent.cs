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
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
            public readonly Dictionary<DamageClass, int> DamageClasses;
            public readonly Dictionary<DamageType, int> DamageTypes;
=======
            public readonly Dictionary<string, int> DamagePerSupportedGroupID;
            public readonly Dictionary<string, int> DamagePerTypeID;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
            public readonly Dictionary<string, int> DamagePerSupportedGroupID;
            public readonly Dictionary<string, int> DamagePerTypeID;
>>>>>>> refactor-damageablecomponent
            public readonly bool IsScanned;

            public MedicalScannerBoundUserInterfaceState(
                EntityUid? entity,
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
                Dictionary<DamageClass, int> damageClasses,
                Dictionary<DamageType, int> damageTypes,
                bool isScanned)
            {
                Entity = entity;
                DamageClasses = damageClasses;
                DamageTypes = damageTypes;
=======
                Dictionary<string, int> damagePerSupportedGroupID,
                Dictionary<string, int> damagePerTypeID,
                bool isScanned)
            {
                Entity = entity;
                DamagePerSupportedGroupID = damagePerSupportedGroupID;
                DamagePerTypeID = damagePerTypeID;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
                Dictionary<string, int> damagePerSupportedGroupID,
                Dictionary<string, int> damagePerTypeID,
                bool isScanned)
            {
                Entity = entity;
                DamagePerSupportedGroupID = damagePerSupportedGroupID;
                DamagePerTypeID = damagePerTypeID;
>>>>>>> refactor-damageablecomponent
                IsScanned = isScanned;
            }

            public bool HasDamage()
            {
<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
                return DamageClasses.Count > 0 || DamageTypes.Count > 0;
=======
                return DamagePerSupportedGroupID.Count > 0 || DamagePerTypeID.Count > 0;
>>>>>>> Refactor damageablecomponent update (#4406)
=======
                return DamagePerSupportedGroupID.Count > 0 || DamagePerTypeID.Count > 0;
>>>>>>> refactor-damageablecomponent
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
