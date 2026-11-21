using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component
{
    [DataField("content"), AutoNetworkedField]
    public string Content { get; set; } = "";

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 10000;

    [DataField("stampedBy"), AutoNetworkedField]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState"), AutoNetworkedField]
    public string? StampState { get; set; }

    [DataField, AutoNetworkedField]
    public bool EditingDisabled;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;

        public PaperBoundUserInterfaceState(string text, List<StampDisplayInfo> stampedBy)
        {
            Text = text;
            StampedBy = stampedBy;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperBeginEditMessage : BoundUserInterfaceMessage
    {
        /// <summary>
        /// The entity ID of the tool used to begin editing (i.e. a pen).
        /// </summary>
        public readonly NetEntity EditToolEntity;

        public PaperBeginEditMessage(NetEntity editTool)
        {
            EditToolEntity = editTool;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        /// <summary>
        /// The entity of the player who edited the paper
        /// </summary>
        public readonly NetEntity User;

        /// <summary>
        /// The entity ID of the tool used to begin editing (i.e. a pen).
        /// </summary>
        public readonly NetEntity EditToolEntity;

        /// <summary>
        /// The new text the paper should have
        /// </summary>
        public readonly string Text;

        public PaperInputTextMessage(NetEntity user, NetEntity editTool, string text)
        {
            User = user;
            EditToolEntity = editTool;
            Text = text;
        }
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
