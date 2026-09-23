using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.UserInterface;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ActivatableUISystem))]
public sealed partial class ActivatableUIComponent : Component
{
    /// <summary>
    /// The UiKey on which the UI will be shown.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(EnumSerializer))]
    public Enum Key;

    /// <summary>
    /// Whether the Ui only opens when the item is held in the hand of the user.
    /// </summary>
    [DataField]
    public bool InHandsOnly;

    /// <summary>
    /// Whether to block others from using the Ui while someone is already using it.
    /// </summary>
    [DataField]
    public bool SingleUser;

    /// <summary>
    /// Whether only admins can access the Ui.
    /// </summary>
    [DataField]
    public bool AdminOnly;

    /// <summary>
    /// The text on the verb for opening the Ui.
    /// </summary>
    [DataField]
    public LocId VerbText = "ui-verb-toggle-open";

    /// <summary>
    /// Whether it's required for the entity to be capable of complex interactions.
    /// </summary>
    [DataField]
    public bool RequiresComplex = true;

    /// <summary>
    /// Items that are required to open this UI.
    /// </summary>
    [DataField]
    public EntityWhitelist? RequiredItems;

    /// <summary>
    /// Whether opening the UI requires an empty hand.
    /// </summary>
    [DataField]
    public bool RequireEmptyHand;

    /// <summary>
    /// If true, then this UI can only be opened via verbs. I.e., normal interactions/activations will not open
    /// the UI.
    /// </summary>
    [DataField]
    public bool VerbOnly;

    /// <summary>
    /// Whether to block non-admin Ghosts from opening/interacting with a UI.
    /// </summary>
    [DataField]
    public bool BlockSpectators;

    /// <summary>
    /// Whether the UI requires an item to be held in the active hand.
    /// </summary>
    /// <remarks> This will only be checked if <see cref="RequiredItems"/> or <see cref="RequireEmptyHand"/> is not null. </remarks>
    [DataField]
    public bool RequireActiveHand = true;

    /// <summary>
    /// The client channel currently using the object, or null if there's none/not single user.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentSingleUser;
}
