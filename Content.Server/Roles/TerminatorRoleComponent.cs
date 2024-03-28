using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Roles;

[RegisterComponent, ExclusiveAntagonist]
public sealed partial class TerminatorRoleComponent : AntagonistRoleComponent
{
    /// <summary>
    /// The delay after objectives are completed at which point the terminator will explode.
    /// </summary>
    [DataField]
    public TimeSpan TerminationDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The time at which termination will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? TerminationTime;
}
