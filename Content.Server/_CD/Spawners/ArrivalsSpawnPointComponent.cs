using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Spawners;

/// <summary>
/// Makes every entity with a job spawn at the point(s) with _jobId, whether latejoining or doing so immediately. 
/// </summary>
[RegisterComponent]
public sealed partial class ArrivalsSpawnPointComponent : Component
{
    /// <summary>
    /// The jobId of the job(s) that should spawn at this point. If null, a (general) spawn point to be used as a fallback if no respective job spawners exist.
    /// </summary>
    [DataField("jobs")]
    public List<string> JobIds = new();
}
