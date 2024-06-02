using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Blinds a person when an item with this component is equipped to the eye, head, or mask slot.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlindfoldComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Override { get; set; } = false;
}
