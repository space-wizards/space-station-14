using Content.Shared.Body.Systems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(LungSystem))]
public sealed partial class LungComponent : Component
{
    [DataField]
    [Access(typeof(LungSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public GasMixture Air = new()
    {
        Volume = 6,
        Temperature = Atmospherics.NormalBodyTemperature
    };

    /// <summary>
    /// The name/key of the solution on this entity which these lungs act on.
    /// </summary>
    [DataField]
    public string SolutionName = "Lung";

    /// <summary>
    /// The solution on this entity that these lungs act on.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// The type of gas this lung needs. Used only for the breathing alerts, not actual metabolism.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "LowOxygen";
}
