using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Humanoid;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedHideableHumanoidLayersSystem))]
public sealed partial class HideableHumanoidLayersComponent : Component
{
    /// <summary>
    ///     A map of the visual layers currently hidden to the equipment
    ///     slots that are currently hiding them. This will affect the base
    ///     sprite on this humanoid layer, and any markings that sit above it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, SlotFlags> HiddenLayers = new();

    /// <summary>
    ///     Client only - which layers were last actually hidden.
    /// </summary>
    [ViewVariables]
    public List<HumanoidVisualLayers> LastHiddenLayers = new();
}

/// <summary>
/// Raised on an entity when one of its humanoid layers changes its visibility
/// </summary>
[ByRefEvent]
public struct HumanoidLayerVisibilityChangedEvent(HumanoidVisualLayers layer, bool visible)
{
    public readonly HumanoidVisualLayers Layer = layer;
    public readonly bool Visible = visible;
    public bool Handled;
}
