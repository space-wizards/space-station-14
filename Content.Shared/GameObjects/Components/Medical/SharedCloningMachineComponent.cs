using System;
using System.Collections.Generic;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using Robust.Shared.Network;

namespace Content.Shared.GameObjects.Components.Medical
{

    public class SharedCloningMachineComponent : Component
    {
        public override string Name => "CloningMachine";

        [Serializable, NetSerializable]
        public class CloningMachineBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly Dictionary<int, string> MindIdName;
            public readonly float Progress;
            public readonly bool MindPresent;

            public CloningMachineBoundUserInterfaceState(Dictionary<int, string> mindIdName, float progress, bool mindPresent)
            {
                MindIdName = mindIdName;
                Progress = progress;
                MindPresent = mindPresent;
            }
        }


        [Serializable, NetSerializable]
        public enum CloningMachineUIKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum CloningMachineVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum CloningMachineStatus
        {
            Idle,
            Cloning,
            Gore,
            NoMind
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Clone
        }

        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;
            public readonly int? ScanId;

            public UiButtonPressedMessage(UiButton button, int? scanId)
            {
                Button = button;
                ScanId = scanId;
            }
        }

    }
}
