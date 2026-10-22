using Robust.Shared.GameStates;

namespace Content.Shared.Chemisry.Components;
/// <summary>
/// Entities with this component cannot be injected, and give a special popup when it is attempted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockInjectionComponent : Component
{
    /// <summary>
    /// LocId of the popup shown when injection is blocked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId FailurePopup = "block-injection-default";
}
