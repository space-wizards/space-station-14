using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that you have the same identity a target for a certain length of time before the round ends.
/// Obviously the agent id will work for this, but it's assumed that you will kill the target to prevent suspicion.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(ImpersonateConditionSystem))]
public sealed partial class ImpersonateConditionComponent : Component
{
    /// <summary>
    /// Name that must match your identity for greentext.
    /// This is stored once after the objective is assigned:
    /// 1. to be a tiny bit more efficient
    /// 2. to prevent the name possibly changing when borging or anything else and messing you up
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? Name;

    /// <summary>
    /// Mind this objective got assigned to, used to continiously checkd impersonation.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? MindId;

    /// <summary>
    /// How long you have to impersonate the target for a greentext.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long you have impersonated the target for.
    /// Increases when your identity is the same, and decreases when it isn't.
    /// Will only increase up to <see cref="Duration"/> so you can't invest time then stop impersonating on evac.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeImpersonated = TimeSpan.Zero;
}
