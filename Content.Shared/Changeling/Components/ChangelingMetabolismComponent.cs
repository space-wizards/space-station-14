using Content.Shared.Metabolism;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Grants the owning entity's <see cref="MetabolizerComponent"/> organs a new metabolism type on map init and whenever they do a changeling transformation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingMetabolismComponent : Component
{
    /// <summary>
    /// The metabolizer type to be added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<MetabolizerTypePrototype> AddedMetabolizer = "Changeling";
}
