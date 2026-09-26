using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's entropic colossus' Sunder ability.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CosmicTileDetonatorComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DetonationTimer = default!;

    [DataField] public TimeSpan DetonateWait = TimeSpan.FromSeconds(0.525);

    [DataField] public EntProtoId TileDetonation = "CosmicSunderExplosionWindup";

    [DataField] public int MaxSize = 5;

    [DataField] public int Size = 1;
}
