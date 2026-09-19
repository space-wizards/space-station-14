using Content.Client.UserInterface.Controls;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Displays a striped list of gases with localized names, mole amounts, and gas color markers.
/// </summary>
public sealed partial class GasDisplayControl : StripedDisplayControl
{
    [Dependency] private IEntityManager _entityManager = default!;
    private readonly SharedAtmosphereSystem _atmosphere;

    public GasDisplayControl()
    {
        IoCManager.InjectDependencies(this);
        _atmosphere = _entityManager.System<SharedAtmosphereSystem>();
    }

    /// <summary>
    /// Adds a gas row using its localized name and color.
    /// </summary>
    /// <param name="gas">The gas and its amount in moles.</param>
    public void AddGas(GasEntry gas)
    {
        var prototype = _atmosphere.GetGas(gas.Gas);
        AddRow(
            Loc.GetString(prototype.Name),
            $"{gas.Amount:0.##} {Loc.GetString("units-mole")}",
            prototype.Color);
    }
}
