using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Physics;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JointVisualsComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("sprite", required: true), AutoNetworkedField]
    public SpriteSpecifier Sprite = default!;

    /// <summary>
    /// Offset from Body A.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("offsetA"), AutoNetworkedField]
    public Vector2 OffsetA;

    /// <summary>
    /// Offset from Body B.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("offsetB"), AutoNetworkedField]
    public Vector2 OffsetB;
}
