using System.Linq;
using Content.Shared.Objectives.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Tracks identities on the mind of the changeling, used for objectives.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingMindIdentityTrackerComponent : Component
{
    /// <summary>
    /// A dictionary of the identities this changeling has devoured.
    /// The key is the string representing the identity, such as the "Name, Job" combination.
    /// The value is whether the identity was granted via devour.
    /// </summary>
    [DataField]
    public Dictionary<string, bool> Identities = new();

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
    public int UniqueDevouredCount => Identities.Count(data => data.Value);
}
