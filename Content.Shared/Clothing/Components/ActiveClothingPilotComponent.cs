using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Back-referencing the pilot of the <see cref="PilotedClothingComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveClothingPilotComponent : Component
{
    /// <summary>
    /// The clothing this pilot is currently operating.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Clothing;
}
