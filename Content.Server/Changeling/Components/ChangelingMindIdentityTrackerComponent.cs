using System.Linq;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Components;

/// <summary>
/// Tracks identities on the mind of the changeling, used for objectives.
/// </summary>
[RegisterComponent, Access(typeof(ChangelingObjectiveSystem))]
public sealed partial class ChangelingMindIdentityTrackerComponent : Component
{
    /// <summary>
    /// A dictionary of the identities this changeling has devoured.
    /// </summary>
    [DataField]
    public List<ChangelingMindTrackedIdentityData> Identities = new();

    /// <summary>
    /// Will append contained identities to the objectives under this issuer.
    /// Won't do anything if null.
    /// </summary>
    [DataField]
    public ProtoId<ObjectiveIssuerPrototype>? AppendIssuer = "Changeling";

    /// <summary>
    /// A count of the unique devours performed by the changeling.
    /// Only counts identities obtained via Devour.
    /// </summary>
    [ViewVariables]
    public int UniqueDevouredCount => Identities.Count(data => data.GrantedDna);
}

/// <summary>
/// Stores data related to an identity a changeling has devoured.
/// Stripped version used for objective purposes and stored on the mind.
/// </summary>
[DataDefinition]
public sealed partial class ChangelingMindTrackedIdentityData
{
    /// <summary>
    /// The original entity that was devoured to obtain this identity.
    /// </summary>
    [DataField]
    public EntityUid? Original; // We never network this to client, so no need to clean this up.

    /// <summary>
    /// Name of the original entity at the time of being devoured.
    /// Used for round-end text.
    /// </summary>
    [DataField]
    public string OriginalName = "Unnamed";

    /// <summary>
    /// Whether this identity has granted DNA after devour.
    /// </summary>
    [DataField]
    public bool GrantedDna;

    /// <summary>
    /// String of the original job of the entity.
    /// Used in round-end text.
    /// </summary>
    [DataField]
    public string OriginalJob;

    /// <summary>
    /// Whether this is the identity the entity started with.
    /// Used in round-end text.
    /// </summary>
    [DataField]
    public bool Starting;
}
