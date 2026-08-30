using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Metabolism;

/// <summary>
/// Grants the owning entity's <see cref="MetabolizerComponent"/> organs a new metabolism type on map init.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MetabolizerSystem))]
public sealed partial class AddMetabolismComponent : Component
{
    /// <summary>
    /// The metabolizer type to be added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<MetabolizerTypePrototype>? AddedMetabolizer;
}
