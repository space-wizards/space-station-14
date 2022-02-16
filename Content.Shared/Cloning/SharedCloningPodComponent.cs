using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Cloning
{
    public class SharedCloningPodComponent : Component
    {
        [Serializable, NetSerializable]
        public class CloningPodBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly Dictionary<int, string?> MindIdName;
            // When this state was created.
            // The reason this is used rather than a start time is because cloning can be interrupted.
            public readonly TimeSpan ReferenceTime;
            // Both of these are in seconds.
            // They're not TimeSpans because of complicated reasons.
            // CurTime of receipt is combined with Progress.
            public readonly float Progress;
            public readonly float Maximum;
            // If true, cloning is progressing (predict clone progress)
            public readonly bool Progressing;
            public readonly bool MindPresent;

            public CloningPodBoundUserInterfaceState(Dictionary<int, string?> mindIdName, TimeSpan refTime, float progress, float maximum, bool progressing, bool mindPresent)
            {
                MindIdName = mindIdName;
                ReferenceTime = refTime;
                Progress = progress;
                Maximum = maximum;
                Progressing = progressing;
                MindPresent = mindPresent;
            }
        }


        [Serializable, NetSerializable]
        public enum CloningPodUIKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum CloningPodVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum CloningPodStatus : byte
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
