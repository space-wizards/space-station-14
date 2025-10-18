using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component
{
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

    [DataField, AutoNetworkedField]
    public bool EditingDisabled;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState(
        string text,
        List<StampDisplayInfo> stampedBy)
        : BoundUserInterfaceState
    {
        public readonly string Text = text;
        public readonly List<StampDisplayInfo> StampedBy = stampedBy;
    }

    [Serializable, NetSerializable]
    public sealed class PaperBeginEditMessage(NetEntity editTool) : BoundUserInterfaceMessage
    {
        public readonly NetEntity EditTool = editTool;
    };

    [Serializable, NetSerializable]
    public sealed class PaperBeginFullEditMessage(NetEntity editTool) : BoundUserInterfaceMessage
    {
        public readonly NetEntity EditTool = editTool;
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage(NetEntity user, NetEntity editTool, string text) : BoundUserInterfaceMessage
    {
        public readonly string Text = text;

        public readonly NetEntity EditTool = editTool;

        public readonly NetEntity User = user;
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputFullTextMessage(string text) : BoundUserInterfaceMessage
    {
        public readonly string Text = text;
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
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
