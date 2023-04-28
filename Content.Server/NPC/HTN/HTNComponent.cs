using System.Threading;
using Content.Server.NPC.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NPC.HTN;

[RegisterComponent, ComponentReference(typeof(NPCComponent))]
public sealed class HTNComponent : NPCComponent
{
    /// <summary>
    /// The base task to use for planning
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("rootTask", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<HTNCompoundTask>))]
    public string RootTask = default!;

    /// <summary>
    /// The NPC's current plan.
    /// </summary>
    [ViewVariables]
    public HTNPlan? Plan;

    // TODO: Need dictionary timeoffsetserializer.
    /// <summary>
    /// Last time we tried a particular <see cref="UtilityService"/>.
    /// </summary>
    [DataField("serviceCooldowns")]
    public Dictionary<string, TimeSpan> ServiceCooldowns = new();

    /// <summary>
    /// How long to wait after having planned to try planning again.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("planCooldown")]
    public float PlanCooldown = 0.45f;

    /// <summary>
    /// How much longer until we can try re-planning. This will happen even during update in case something changed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PlanAccumulator = 0f;

    [ViewVariables]
    public HTNPlanJob? PlanningJob = null;

    [ViewVariables]
    public CancellationTokenSource? PlanningToken = null;

    /// <summary>
    /// Is this NPC currently planning?
    /// </summary>
    [ViewVariables] public bool Planning => PlanningJob != null;
}
