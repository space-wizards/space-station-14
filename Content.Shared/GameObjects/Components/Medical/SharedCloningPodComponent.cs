#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Medical
{
    public class SharedCloningPodComponent : Component
    {
        public override string Name => "CloningPod";

        [Serializable, NetSerializable]
        public class CloningPodBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly Dictionary<int, string?> MindIdName;
            public readonly float Progress;
            public readonly bool MindPresent;

            public CloningPodBoundUserInterfaceState(Dictionary<int, string?> mindIdName, float progress, bool mindPresent)
            {
                MindIdName = mindIdName;
                Progress = progress;
                MindPresent = mindPresent;
            }
        }


        [Serializable, NetSerializable]
        public enum CloningPodUIKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum CloningPodVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum CloningPodStatus
        {
            Idle,
            Cloning,
            Gore,
            NoMind
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Clone,
            Eject
        }

        [Serializable, NetSerializable]
        public class CloningPodUiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;
            public readonly int? ScanId;

            public CloningPodUiButtonPressedMessage(UiButton button, int? scanId)
            {
                Button = button;
                ScanId = scanId;
            }
        }

    }
}
