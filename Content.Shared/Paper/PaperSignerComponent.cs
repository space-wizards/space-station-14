using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Paper;


/// <summary>
/// Indicates that an item (usually a pen) can be used to sign things
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PaperSignerComponent : Component
{

    /// <summary>
    ///     The sprite state of the stamp to display on the paper from paper Sprite path.
    /// </summary>
    [DataField]
    public string SignState { get; set; } = "paper_signature";

    /// <summary>
    /// The sound produced when signing
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    /// <summary>
    /// When true, allows modifying the string that will be signed with instead of using the entity's name
    /// </summary>
    [DataField]
    public bool Customize = false;

    /// <summary>
    /// When Customize is true, this signature will be used instead of the entity's name, and it will be customizable via a UI
    /// </summary>
    [DataField]
    public string Signature = "";

    /// <summary>
    /// The color of the ink used on UIs
    /// </summary>
    [DataField]
    public Color SignatureColor = Color.FromHex("#000000FF");

    /// <summary>
    /// Maximum length of the signature field
    /// </summary>
    [DataField]
    public int MaxSignatureLength = 30;

    /// <summary>
    /// If greater than 0, then a doafter will be used for the signing action
    /// </summary>
    [DataField]
    public TimeSpan DoAfterLength = TimeSpan.Zero;

    /// <summary>
    /// If non-null, then this will displayed once the doafter begins (if it does begin, that is)
    /// </summary>
    [DataField]
    public LocId? DoAfterPopupSelf;

    /// <summary>
    /// Same as DoAfterPopupSelf but shown to others.
    /// </summary>
    [DataField]
    public LocId? DoAfterPopupOthers;
}

[Serializable, NetSerializable]
public sealed class PaperSignerComponentState(string signature) : IComponentState
{
    public string Signature = signature;
}

[Serializable, NetSerializable]
public enum SignatureUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SignatureChangedMessage(string signature) : BoundUserInterfaceMessage
{
    public string Signature { get; } = signature;
}
