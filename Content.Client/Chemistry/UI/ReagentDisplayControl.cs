using Content.Client.UserInterface.Controls;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Displays a striped reagent list with reagent color markers.
/// </summary>
public sealed partial class ReagentDisplayControl : StripedDisplayControl
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public ReagentDisplayControl()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Adds a reagent row using the localized reagent name and color.
    /// </summary>
    /// <param name="reagent">The reagent to display.</param>
    /// <param name="quantity">The quantity of reagent to display.</param>
    /// <param name="trailingControls">Optional controls appended to the row.</param>
    public void AddReagent(ReagentId reagent, FixedPoint2 quantity, IEnumerable<Control>? trailingControls = null)
    {
        _prototypeManager.TryIndex(reagent.Prototype, out var prototype);
        var name = prototype?.LocalizedName ?? Loc.GetString("chem-master-window-unknown-reagent-text");
        AddRow(name, $"{quantity}{Loc.GetString("units-u")}", prototype?.SubstanceColor, trailingControls);
    }

    /// <summary>
    /// Adds a row for a non-reagent entity.
    /// </summary>
    /// <param name="name">The name to display.</param>
    /// <param name="quantity">The quantity to display.</param>
    /// <param name="trailingControls">Optional controls appended to the row.</param>
    public void AddEntity(string name, FixedPoint2 quantity, IEnumerable<Control>? trailingControls = null)
    {
        AddRow(name, $"{quantity}{Loc.GetString("units-u")}", null, trailingControls);
    }
}
