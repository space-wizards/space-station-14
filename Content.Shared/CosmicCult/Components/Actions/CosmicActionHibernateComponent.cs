using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]

public sealed partial class CosmicActionHibernateComponent : Component
{
    [DataField]
    public TimeSpan SlumberTime = TimeSpan.FromSeconds(5); // TODO: Should be 20 seconds.
}
