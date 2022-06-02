using Content.Server.Atmos;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Body.Components;

[RegisterComponent, Friend(typeof(LungSystem))]
public sealed class LungComponent : Component
{
    [DataField("air")]
    [Friend(typeof(LungSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public GasMixture Air { get; set; } = new()
    {
        Volume = 6,
        Temperature = Atmospherics.NormalBodyTemperature
    };

    [ViewVariables]
    [Friend(typeof(LungSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public Solution LungSolution = default!;
}
