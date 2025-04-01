using Content.Shared.Atmos.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Contains layer data for atmos pipes. Layers allow multiple atmos pipes with the 
/// same orientation to be anchored to the same tile without their contents mixing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAtmosPipeLayersSystem))]
public sealed partial class AtmosPipeLayersComponent : Component
{
    /// <summary>
    /// The number of pipe layers this entity supports.
    /// </summary>
    [DataField]
    public byte NumberOfPipeLayers = 3;

    /// <summary>
    /// Determines which layer the pipe is currently assigned.
    /// Only pipes on the same layer can connect with each other.
    /// </summary>
    [DataField("pipeLayer"), AutoNetworkedField]
    public byte CurrentPipeLayer = 0;

    /// <summary>
    /// The RSI path that the entity sprite will use for each pipe layer.
    /// If null, the RSI path does not differ between pipe layers.
    /// </summary>
    /// <remarks>
    /// If not null, it is assumed that there will be an array entry for each pipe layer
    /// (from 0 to <see cref="NumberOfPipeLayers"/> - 1).
    /// </remarks>
    [DataField]
    public string[]? SpriteRsiPaths = null;

    /// <summary>
    /// A dictionary of entity sprite layers that have their
    /// RSI paths updated when the pipe layer changes.
    /// </summary>
    /// <remarks>
    /// It is assumed that there will be an array entry for each pipe layer
    /// (from 0 to <see cref="NumberOfPipeLayers"/> - 1).
    /// </remarks>
    [DataField]
    public Dictionary<string, string[]> SpriteLayersRsiPaths = new();

    /// <summary>
    /// The pipe layers of this entity cannot be changed when this value is true. 
    /// </summary>
    [DataField]
    public bool PipeLayersLocked = false;

    /// <summary>
    /// Tool quality required to cause a pipe to change layers
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> Tool = "Screwing";

    /// <summary>
    /// The base delay to use for changing layers.
    /// </summary>
    [DataField]
    public float Delay = 1f;
}

/// <summary>
/// Raised when a player attempts to cycle a pipe to its next layer
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TryCyclingPipeLayerCompletedEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised when a player attempts to set a pipe a specified layer
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrySettingPipeLayerCompletedEvent : SimpleDoAfterEvent
{
    public int PipeLayer;

    public TrySettingPipeLayerCompletedEvent(int pipeLayer)
    {
        PipeLayer = pipeLayer;
    }
}

[Serializable, NetSerializable]
public enum AtmosPipeLayerVisuals
{
    Sprite,
    SpriteLayers,
    DrawDepth,
}
