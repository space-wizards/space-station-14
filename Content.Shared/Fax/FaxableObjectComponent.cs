using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Shared.Fax;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaxableObjectComponent : Component
{
    /// <summary>
    /// Sprite to use when inserting an object.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public string InsertingState = "inserting";
}
