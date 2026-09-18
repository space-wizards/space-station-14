using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Humanoid;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedHideableHumanoidLayersSystem))]
public sealed partial class HideableHumanoidLayersComponent : Component
{
    /// <summary>
    /// A map of the visual layers currently hidden to the equipment
    /// slots that are currently hiding them. This will affect the base
    /// sprite on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, SlotFlags> HiddenLayers = new();

    /// <summary>
    /// Client only - which layers were last actually hidden.
    /// </summary>
    [ViewVariables]
    public HashSet<HumanoidVisualLayers> LastHiddenLayers = new();
}

/// <summary>
/// Raised on an entity before one of its humanoid layers changes its visibility.
/// If <paramref name="visible"/> is false, this event is a request to hide the layer.
/// If true, the layer will be shown again regardless.
/// </summary>
[ByRefEvent]
public struct HumanoidLayerVisibilityChangedEvent(HumanoidVisualLayers layer, bool visible)
{
    /// <summary>The layer whose visibility will change.</summary>
    public readonly HumanoidVisualLayers Layer = layer;
    /// <summary>The new visibility of the layer.</summary>
    public readonly bool Visible = visible;
    /// <summary>When Visible is true, set this true to allow the layer to be hidden.</summary>
    public bool ShouldHide;
}
