using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Added to mind role entities to tag that they are the cosmic cult leader.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCosmicCultSystem))]
public sealed partial class CosmicCultLeadComponent : Component
{
    /// <summary>
    /// The status icon corresponding to the lead cultist.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "CosmicCultLeadIcon";

    /// <summary>
    /// How long the stun will last after the user is converted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3);

    public override bool SessionSpecific => true;

    [DataField]
    public EntProtoId MonumentPrototype = "MonumentCosmicCultSpawnIn";

    [DataField]
    public EntProtoId CosmicMonumentPlaceAction = "ActionCosmicPlaceMonument";

    [DataField]
    public EntityUid? CosmicMonumentPlaceActionEntity;

    [DataField]
    public EntProtoId CosmicMonumentMoveAction = "ActionCosmicMoveMonument";

    [DataField]
    public EntityUid? CosmicMonumentMoveActionEntity;

}

// CosmicCultLeadComponent
