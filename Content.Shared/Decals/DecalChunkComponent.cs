using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Decals;

/// <summary>
/// Networked decal data attached to a chunk entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class DecalChunkComponent : Component
{
    public const ushort PredictedDecalCount = 256;
    public const ushort MaxServerDecalId = ushort.MaxValue - PredictedDecalCount;
    public const ushort MinPredictedDecalId = MaxServerDecalId + 1;

    /// <summary>
    /// Client predicts entities from top of the chunk index down while server goes bottom-up.
    /// This way we can minimize chances of overlap and be non-destructive to server states.
    /// </summary>
    public ushort NextPredictedDecal = ushort.MaxValue;

    [AutoNetworkedField]
    [DataField(customTypeSerializer: typeof(DecalChunkDecalsSerializer))]
    public Dictionary<ushort, Decal> Decals = new();

    /// <summary>
    /// Highest authoritative decal ID allocated in this chunk.
    /// </summary>
    [DataField]
    public ushort MaxDecalId;

    public List<ushort> FreeDecalIds = new();
}
