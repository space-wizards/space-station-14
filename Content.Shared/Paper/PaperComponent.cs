using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component
{
    public PaperAction Mode;
    [DataField, AutoNetworkedField]
    public string Content { get; set; } = "";

    [DataField]
    public int ContentSize { get; set; } = 10000;

    [DataField, AutoNetworkedField]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? StampState { get; set; }

    /// <summary>
    /// Whether the paper is editable, protected or locked.
    /// Protected paper can only be edited by things with a specific tag.
    /// Locked paper can never be edited.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PaperLockStatus EditingState = PaperLockStatus.Editable;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;

        public PaperBoundUserInterfaceState(string text, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public PaperInputTextMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}

[Serializable, NetSerializable]
public enum PaperLockStatus : byte
{
    Editable, // Can be freely edited
    Protected, // Can only be edited by things with a specific tag
    Locked // Can NEVER be edited
}
