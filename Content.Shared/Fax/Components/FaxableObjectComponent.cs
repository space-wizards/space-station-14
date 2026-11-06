using Robust.Shared.GameStates;

namespace Content.Shared.Fax.Components;
/// <summary>
/// Entity with this component can be faxed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaxableObjectComponent : Component
{
    /// <summary>
    /// Sprite to use when inserting an object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? InsertingState;
}
