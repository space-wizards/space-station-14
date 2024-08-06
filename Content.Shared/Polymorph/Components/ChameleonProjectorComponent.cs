using Content.Shared.Polymorph;
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
    /// Polymorph configuration for the disguise entity.
    /// </summary>
    [DataField(required: true)]
    public PolymorphConfiguration Polymorph = new();

    /// <summary>
    /// Action for disabling your disguise's rotation.
    /// </summary>
    [DataField]
    public EntProtoId NoRotAction = "ActionDisguiseNoRot";

    /// <summary>
    /// Action for anchoring your disguise in place.
    /// </summary>
    [DataField]
    public EntProtoId AnchorAction = "ActionDisguiseAnchor";

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
    /// Popup shown to the user when they try to disguise as an invalid entity.
    /// </summary>
    [DataField]
    public LocId InvalidPopup = "chameleon-projector-invalid";

    /// <summary>
    /// Popup shown to the user when they disguise as a valid entity.
    /// </summary>
    [DataField]
    public LocId SuccessPopup = "chameleon-projector-success";
}
