using Content.Shared.Polymorph.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Polymorph.Components;

/// <summary>
/// A chameleon projector polymorphs you into a clicked entity, then polymorphs back when clicked on or destroyed.
/// This creates a new dummy polymorph entity and copies the appearance over.
/// </summary>
[RegisterComponent, Access(typeof(SharedChameleonProjectorSystem))]
public sealed partial class ChameleonProjectorComponent : Component
{
    /// <summary>
    /// If non-null, whitelist for valid entities to disguise as.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If non-null, blacklist that prevents entities from being used even if they are in the whitelist.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Disguise entity to spawn and use.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId DisguiseProto = string.Empty;

    /// <summary>
    /// Action for disabling your disguise's rotation.
    /// </summary>
    [DataField]
    public EntProtoId NoRotAction = "ActionDisguiseNoRot";
    [DataField]
    public EntityUid? NoRotActionEntity;

    /// <summary>
    /// Action for anchoring your disguise in place.
    /// </summary>
    [DataField]
    public EntProtoId AnchorAction = "ActionDisguiseAnchor";
    [DataField]
    public EntityUid? AnchorActionEntity;

    /// <summary>
    /// Minimum health to give the disguise.
    /// </summary>
    [DataField]
    public float MinHealth = 1f;

    /// <summary>
    /// Maximum health to give the disguise, health scales with mass.
    /// </summary>
    [DataField]
    public float MaxHealth = 100f;

    /// <summary>
    /// User currently disguised by this projector, if any
    /// </summary>
    [DataField]
    public EntityUid? Disguised;
}
