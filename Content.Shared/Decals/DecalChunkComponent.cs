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
