using Content.Server.AbstractAnalyzer;
using Content.Server.Botany.Systems;
using Content.Server.Medical.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Botany.Components;

/// <inheritdoc/>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(PlantAnalyzerSystem))]
public sealed partial class PlantAnalyzerComponent : AbstractAnalyzerComponent
{
    /// <inheritdoc/>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public override TimeSpan NextUpdate { get; set; } = TimeSpan.Zero;
}
