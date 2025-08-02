using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.PAI;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PAICustomizationComponent : Component
{
    /// <summary>
    /// The current emotion of the PAI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PAIEmotion CurrentEmotion = PAIEmotion.Neutral;

    /// <summary>
    /// The current glasses type of the PAI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PAIGlasses CurrentGlasses = PAIGlasses.None;
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

/// <summary>
/// Represents the glasses types a PAI can wear.
/// </summary>
[Serializable, NetSerializable]
public enum PAIGlasses : byte
{
    None,
    Glasses,
    Sunglasses
}

[Serializable, NetSerializable]
public enum PAIEmotionVisuals : byte
{
    Emotion
}

[Serializable, NetSerializable]
public enum PAIGlassesVisuals : byte
{
    Glasses
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
public sealed class PAIGlassesMessage : BoundUserInterfaceMessage
{
    public readonly PAIGlasses Glasses;

    public PAIGlassesMessage(PAIGlasses glasses)
    {
        Glasses = glasses;
    }
}

[Serializable, NetSerializable]
public sealed class PAISetNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public PAISetNameMessage(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class PAIResetNameMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum PAICustomizationUiKey : byte
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

[Serializable, NetSerializable]
public sealed class PAIGlassesStateMessage : BoundUserInterfaceMessage
{
    public readonly PAIGlasses Glasses;

    public PAIGlassesStateMessage(PAIGlasses glasses)
    {
        Glasses = glasses;
    }
}

[Serializable, NetSerializable]
public sealed class PAINameStateMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public PAINameStateMessage(string name)
    {
        Name = name;
    }
}
