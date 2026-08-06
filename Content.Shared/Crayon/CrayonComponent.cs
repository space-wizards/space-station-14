using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon;

/// <summary>
/// Component holding the state of a crayon-like component
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCrayonSystem))]
public sealed partial class CrayonComponent : Component
{
    /// <summary>
    /// The ID of currently selected decal prototype that will be placed when the crayon is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SelectedState;

    /// <summary>
    /// Color with which the crayon will draw.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color;

    /// <summary>
    /// Play a sound when drawing if specified.
    /// </summary>
    [DataField]
    public SoundSpecifier? UseSound;

    /// <summary>
    /// Can the color can be changed?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SelectableColor;

    /// <summary>
    /// Should the crayon be deleted when all charges are consumed?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteEmpty = true;
}

/// <summary>
/// Opens the crayon window for decal and color selection.
/// </summary>
[Serializable, NetSerializable]
public enum CrayonUiKey : byte
{
    Key,
}

/// <summary>
/// Used by the client to notify the server about the selected decal ID
/// </summary>
[Serializable, NetSerializable]
public sealed class CrayonSelectMessage : BoundUserInterfaceMessage
{
    public readonly string State;
    public CrayonSelectMessage(string selected)
    {
        State = selected;
    }
}

/// <summary>
/// Sets the color of the crayon, used by Rainbow Crayon
/// </summary>
[Serializable, NetSerializable]
public sealed class CrayonColorMessage : BoundUserInterfaceMessage
{
    public readonly Color Color;
    public CrayonColorMessage(Color color)
    {
        Color = color;
    }
}

/// <summary>
/// Server to CLIENT. Notifies the BUI that a decal with given ID has been drawn.
/// Allows the client UI to advance forward in the client-only ephemeral queue,
/// preventing the crayon from becoming a magic text storage device.
/// </summary>
[Serializable, NetSerializable]
public sealed class CrayonUsedMessage : BoundUserInterfaceMessage
{
    public readonly string DrawnDecal;

    public CrayonUsedMessage(string drawn)
    {
        DrawnDecal = drawn;
    }
}

/// <summary>
/// The state of the crayon UI as sent by the server
/// </summary>
/// <summary>
/// TODO: Delete this and use component states and predict the UI.
/// This info is already networked on its own.
/// </summary>
[Serializable, NetSerializable]
public sealed class CrayonBoundUserInterfaceState : BoundUserInterfaceState
{
    public string Selected;
    /// <summary>
    /// Can the color can be changed
    /// </summary>
    public bool SelectableColor;
    public Color Color;

    public CrayonBoundUserInterfaceState(string selected, bool selectableColor, Color color)
    {
        Selected = selected;
        SelectableColor = selectableColor;
        Color = color;
    }
}
