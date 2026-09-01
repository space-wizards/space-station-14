using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Spreader;

/// <summary>
/// Added to entities being considered for spreading via <see cref="SpreaderSystem"/>.
/// This needs to be manually added and removed.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveEdgeSpreaderComponent : Component
{
    /// <summary>
    /// Time at which the next spread will occur.
    /// This is automatically set when the spreader activates
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpreadTime;
}
