using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Physics;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JointVisualsComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("sprite", required: true), AutoNetworkedField]
    public SpriteSpecifier Sprite = default!;
}
