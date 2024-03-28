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
    /// User interface to add that the action opens.
    /// </summary>
    [DataField(required: true)]
    public PrototypeData Interface = new();

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
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
