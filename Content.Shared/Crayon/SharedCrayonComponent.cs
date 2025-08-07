using Content.Shared.Decals;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.Crayon;

/// <summary>
/// Component holding the state of a crayon-like component
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, Access(typeof(SharedCrayonSystem))]
public sealed partial class CrayonComponent : Component
{
    /// <summary>
    /// The ID of currently selected decal prototype that will be placed when the crayon is used
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DecalPrototype> SelectedState = new("like");

    /// <summary>
    /// Color with which the crayon will draw
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color;

    [DataField]
    public SoundSpecifier? UseSound;

    /// <summary>
    /// Can color be changed by the player
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool SelectableColor;

    /// <summary>
    /// Number of uses the crayon currenty has
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public int Charges;

    /// <summary>
    /// Maximum number of uses for given crayon
    /// <summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public int Capacity = 30;

    /// <summary>
    /// Should crayon be deleted when charges run out
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool DeleteEmpty = true;
}

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
    public readonly ProtoId<DecalPrototype> DrawnDecal;

    public CrayonUsedMessage(ProtoId<DecalPrototype> drawn)
    {
        DrawnDecal = drawn;
    }
}

/// <summary>
/// Component state, describes how many charges are left in the crayon in the near-hand UI
/// </summary>
[Serializable, NetSerializable]
public sealed class CrayonComponentState : ComponentState
{
    public readonly Color Color;
    public readonly string State;
    public readonly int Charges;
    public readonly int Capacity;

    public CrayonComponentState(Color color, string state, int charges, int capacity)
    {
        Color = color;
        State = state;
        Charges = charges;
        Capacity = capacity;
    }
}

/// <summary>
/// The state of the crayon UI as sent by the server
/// </summary>
[Serializable, NetSerializable]
public sealed class CrayonBoundUserInterfaceState : BoundUserInterfaceState
{
    public ProtoId<DecalPrototype>? Selected;
    /// <summary>
    /// Whether or not the color can be selected
    /// </summary>
    public bool SelectableColor;
    public Color Color;

    public CrayonBoundUserInterfaceState(ProtoId<DecalPrototype>? selected, bool selectableColor, Color color)
    {
        Selected = selected;
        SelectableColor = selectableColor;
        Color = color;
    }
}
