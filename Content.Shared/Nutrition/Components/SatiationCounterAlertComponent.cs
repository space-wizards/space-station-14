using Content.Shared.Alert.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Used for alerts that wish to display their satiation value using <see cref="GenericCounterAlertComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SatiationCounterAlertComponent : Component
{
    /// <summary>
    /// The satiation type whose count should be used for the alert.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType = SatiationSystem.Hunger;
}
