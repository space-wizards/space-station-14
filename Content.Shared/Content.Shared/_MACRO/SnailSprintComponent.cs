using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._MACRO.Species;

/// <summary>
///     Allows an entity to use thirst for a speed boost.
///     Also allows that speed boost to produce a fluid.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSnailSprintSystem)), AutoGenerateComponentState]
public sealed partial class SnailSprintComponent : Component
{
    /// <summary>
    ///     The entity needed to perform the action.
    ///     Granted upon the creation of the entity.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Action;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     Minimum thirst threshold required to perform the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SatiationValue MinThirstThreshold = "Okay";

    /// <summary>
    ///     The amount of thirst to be taken at the end of the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThirstCost = 10f;

    /// <summary>
    ///     Popup text for when the action fails due to not having enough thirst.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId FailedPopup = "snailsprint-failure-thirst";

    /// <summary>
    ///     The length of the doafter in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SprintLength = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The speed boost to be applied. Multiplicative.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedBoost = 1.0f;

    /// <summary>
    ///     The name of the reagent produced.
    ///     This should be a ReagentId, but I don't care enough to figure out why it doesn't work like that.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? ReagentProduced = null;

    /// <summary>
    ///     The ammount of reagent to be produced.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReagentQuantity = 15f;

    /// <summary>
    ///     Whether or not the action has been activated, and has not ended yet.
    /// </summary>
    public bool Active = false;

    /// <summary>
    ///     The last tile the entity has stepped on.
    /// </summary>
    public TileRef LastTile = TileRef.Zero;
}
