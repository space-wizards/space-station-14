using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Devices
{
    [RegisterComponent]
    public class SharedVoiceAnalyzerComponent : Component
    {
        public override string Name => "VoiceAnalyzer";

        public AnalyzeMode Mode = AnalyzeMode.Inclusive;
        public enum AnalyzeMode
        {
            Inclusive,
            Exclusive,
            Recognizer,
            VoiceSensor
        }

    }

    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum VoiceAnalyzerUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents a <see cref="SharedSignalerComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public class VoiceAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
    {

        public VoiceAnalyzerBoundUserInterfaceState(int frequency)
        {
        }

    }

    [Serializable, NetSerializable]
    public class VoiceAnalyzerUpdateModeMessage : BoundUserInterfaceMessage
    {
        public int ModeEnum { get; }

        public VoiceAnalyzerUpdateModeMessage( int modeEnum )
        {
            ModeEnum = modeEnum;
        }
    }

    [Serializable, NetSerializable]
    public class VoiceAnalyzerUpdateTextMessage : BoundUserInterfaceMessage
    {
        public string VoiceText { get; }

        public VoiceAnalyzerUpdateTextMessage( string voiceText )
        {
            VoiceText = voiceText;
        }
    }

}
