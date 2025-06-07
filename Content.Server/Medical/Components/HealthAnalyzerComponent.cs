using Content.Shared.AbstractAnalyzer;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.Components;

/// <inheritdoc/>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(HealthAnalyzerSystem), typeof(CryoPodSystem))]
public sealed partial class HealthAnalyzerComponent : AbstractAnalyzerComponent
{
    /// <inheritdoc/>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public override TimeSpan NextUpdate { get; set; } = TimeSpan.Zero;
}
