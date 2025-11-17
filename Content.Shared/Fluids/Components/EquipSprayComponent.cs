using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Allows items with the spray component to be equipped and sprayable with a unique action.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EquipSprayComponent : Component
{
    /// <summary>
    /// The action that gets displayed when the sprayer is equipped.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionShootWater";

    /// <summary>
    /// Reference to the action.
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Verb locid that will come up when interacting with the sprayer. Set to null for no verb!
    /// </summary>
    [DataField]
    public LocId? VerbLocId;
}
