using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.PAI;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PAIEmotionComponent : Component
{
    /// <summary>
    /// The current emotion of the PAI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PAIEmotion CurrentEmotion = PAIEmotion.Neutral;
}

/// <summary>
/// Represents the emotions a PAI can express.
/// </summary>
[Serializable, NetSerializable]
public enum PAIEmotion : byte
{
    Neutral,
    Happy,
    Sad,
    Angry
}

[Serializable, NetSerializable]
public enum PAIEmotionVisuals : byte
{
    Emotion
}

[Serializable, NetSerializable]
public sealed class PAIEmotionMessage : BoundUserInterfaceMessage
{
    public readonly PAIEmotion Emotion;

    public PAIEmotionMessage(PAIEmotion emotion)
    {
        Emotion = emotion;
    }
}

[Serializable, NetSerializable]
public enum PAIEmotionUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PAIEmotionStateMessage : BoundUserInterfaceMessage
{
    public readonly PAIEmotion Emotion;

    public PAIEmotionStateMessage(PAIEmotion emotion)
    {
        Emotion = emotion;
    }
}
