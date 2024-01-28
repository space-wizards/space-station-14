using Robust.Shared.Prototypes;

namespace Content.Shared.Prototypes.Components;

/// <summary>
/// Component to specify other components to remove in response to a variety of events.
/// </summary>
[RegisterComponent]
public sealed partial class ComponentRemoverComponent : Component
{
    /// <summary>
    /// Perform the remove in response to OnMapInit.
    /// </summary>
    [DataField("doOnMapInit")]
    public bool DoOnMapInit = false;

    /// <summary>
    /// Components to remove.
    /// </summary>
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();
}
