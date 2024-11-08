using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeltaV.Recruiter;

/// <summary>
/// Pen that can be pricked with the user's blood, and requires blood to sign papers.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRecruiterPenSystem))]
[AutoGenerateComponentState]
public sealed partial class RecruiterPenComponent : Component
{
    /// <summary>
    /// Solution on the pen to draw blood to and use for signing.
    /// </summary>
    [DataField]
    public string Solution = "blood";

    /// <summary>
    /// Mind of the recruiter this pen belongs to.
    /// Used for objective and is set when first picked up.
    /// </summary>
    [DataField]
    public EntityUid? RecruiterMind;

    /// <summary>
    /// Lets other clients predict recruiter being bound without knowing who it is.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Bound;

    /// <summary>
    /// Entities of every person that signed paper with this pen.
    /// Used to prevent someone gaming it by signing multiple papers.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Recruited = new();

    /// <summary>
    /// If the user matches this blacklist they can't use this pen.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// If the user's mind matches this blacklist they can't use this pen.
    /// </summary>
    [DataField]
    public EntityWhitelist? MindBlacklist;
}
