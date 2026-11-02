using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Factions;

/// <summary>
/// Which CarpFinder faction this creature belongs to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFFactionComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CFFactionPrototype> Faction = "Neutral";
}
