using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Uses battery charge to spawn an item and place it in the user's hands.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedItemCreatorSystem))]
public sealed partial class ItemCreatorComponent : Component
{
    /// <summary>
    /// The battery entity to use charge from
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Battery;

    /// <summary>
    /// The action id for creating an item.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<InstantActionComponent> Action = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Battery charge used to create an item.
    /// </summary>
    [DataField(required: true)]
    public float Charge = 14.4f;

    /// <summary>
    /// Item to create with the action
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnedPrototype = string.Empty;

    /// <summary>
    /// Popup shown to the user when there isn't enough power to create an item.
    /// </summary>
    [DataField(required: true)]
    public LocId NoPowerPopup = string.Empty;
}

/// <summary>
/// Action event to use an <see cref="ItemCreator"/>.
/// </summary>
public sealed partial class CreateItemEvent : InstantActionEvent;
