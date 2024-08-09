using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.PAI;

/// <summary>
/// Allows this board to be used as a pAI expansion card.
/// Once installed permanently adds components and an action to a pAI.
/// </summary>
[RegisterComponent, Access(typeof(PAIExpansionSystem))]
public sealed partial class PAIExpansionCardComponent : Component
{
    /// <summary>
    /// Action to give the pAI once installed.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action = new();

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Components to add to the pAI once installed.
    /// </summary>
    /// <remarks>
    /// You don't have to worry about how they get removed since cards can't be removed.
    /// All you need to manage is how it works with existing components,
    /// e.g. for ui <c>AddUserInterfaceComponent</c> instead of bulldozing midi, map etc.
    /// </remarks>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Whitelist the pAI must match to be installed.
    /// No whitelist means any pAI can use this card.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Popup shown when trying to insert this card into a pAI that doesn't match the whitelist.
    /// </summary>
    [DataField]
    public LocId WhitelistFailPopup = "pai-expansion-card-whitelist-fail";
}
